using Microsoft.EntityFrameworkCore;
using MyTrader.Core.Models;
using MyTrader.Infrastructure.Data;

namespace MyTrader.Api.Services;

public class BacktestServiceSimple
{
    private readonly TradingDbContext _db;

    public BacktestServiceSimple(TradingDbContext db)
    {
        _db = db;
    }

    public async Task<BacktestRunResult> RunAsync(BacktestRunRequest req, CancellationToken ct)
    {
        // Resolve or create SymbolId
        var venue = req.Venue ?? "BINANCE";
        var symbolRow = await _db.Symbols.FirstOrDefaultAsync(s => s.Ticker == req.Symbol && s.Venue == venue, ct);
        if (symbolRow == null)
        {
            symbolRow = new Symbol { Id = Guid.NewGuid(), Ticker = req.Symbol, Venue = venue, AssetClass = "CRYPTO", IsActive = true, IsTracked = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
            _db.Symbols.Add(symbolRow);
            await _db.SaveChangesAsync(ct);
        }

        // Load candles
        var start = req.StartUtc ?? DateTime.UtcNow.AddDays(-30);
        var end = req.EndUtc ?? DateTime.UtcNow;
        var candles = await _db.Candles
            .Where(c => c.SymbolId == symbolRow.Id && c.Timeframe == req.Timeframe && c.OpenTime >= start && c.OpenTime <= end)
            .OrderBy(c => c.OpenTime)
            .ToListAsync(ct);

        if (candles.Count < 50)
        {
            return new BacktestRunResult { Message = "Not enough candles", CandleCount = candles.Count };
        }

        // Compute indicators (SMA, EMA, RSI, Bollinger)
        var closes = candles.Select(c => c.Close).ToList();
        var bbPeriod = req.BollingerPeriod ?? 20;
        var bbMult = req.BollingerStdDev ?? 2m;
        var rsiPeriod = req.RsiPeriod ?? 14;

        var sma = SMA(closes, 20);
        var ema12 = EMA(closes, 12);
        var ema26 = EMA(closes, 26);
        var macd = ema12.Zip(ema26, (a, b) => a - b).ToList();
        var macdSignal = EMA(macd, 9);
        var macdHist = macd.Zip(macdSignal, (m, s) => m - s).ToList();
        var rsi = RSI(closes, rsiPeriod);
        var (bbU, bbM, bbL) = Bollinger(closes, bbPeriod, bbMult);

        // Simple strategy: BB + RSI + MACD confirmation
        decimal capital = req.InitialCapital ?? 10000m;
        decimal cash = capital;
        decimal positionQty = 0m;
        decimal lastEntryPrice = 0m;
        decimal peakEquity = capital;
        decimal maxDrawdown = 0m;
        int wins = 0, losses = 0, totalTrades = 0;

        for (int i = 30; i < candles.Count; i++)
        {
            var price = closes[i];
            var r = rsi[i];
            var u = bbU[i];
            var l = bbL[i];
            var h = macdHist[i];

            var equity = cash + positionQty * price;
            if (equity > peakEquity) peakEquity = equity;
            var dd = (peakEquity - equity) / (peakEquity == 0 ? 1 : peakEquity);
            if (dd > maxDrawdown) maxDrawdown = dd;

            // Entry: price near lower band and RSI < 35 and MACD hist rising
            if (positionQty == 0 && price <= l && r < 35 && h > 0)
            {
                var riskPct = req.RiskPct ?? 0.1m; // 10%
                var toInvest = equity * riskPct;
                positionQty = toInvest / price;
                cash -= toInvest;
                lastEntryPrice = price;
                totalTrades++;
            }
            // Exit: price near upper band or RSI > 65 or MACD hist falling
            else if (positionQty > 0 && (price >= u || r > 65 || h < 0))
            {
                var proceeds = positionQty * price;
                var pnl = proceeds - (positionQty * lastEntryPrice);
                if (pnl >= 0) wins++; else losses++;
                cash += proceeds;
                positionQty = 0;
            }
        }

        var finalEquity = cash + positionQty * closes.Last();
        var totalReturn = finalEquity - capital;
        var totalReturnPct = capital == 0 ? 0 : totalReturn / capital;
        var winRate = (wins + losses) == 0 ? 0 : (decimal)wins / (wins + losses);

        // Persist results if requested
        Guid? resultId = null;
        if (req.Persist)
        {
            // ensure system user & strategy
            var user = await EnsureSystemUserAsync(ct);
            var strategy = await EnsureStrategyAsync(user.Id, req, ct);

            var result = new BacktestResults
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                StrategyId = strategy.Id,
                SymbolId = symbolRow.Id,
                Timeframe = req.Timeframe,
                StartingCapital = capital,
                EndingCapital = finalEquity,
                TotalReturn = totalReturn,
                TotalReturnPercentage = Math.Round(totalReturnPct * 100, 4),
                MaxDrawdown = Math.Round(maxDrawdown * capital, 8),
                MaxDrawdownPercentage = Math.Round(maxDrawdown * 100, 4),
                SharpeRatio = 0,
                WinRate = Math.Round(winRate * 100, 4),
                DetailedResults = null,
                StrategyConfig = null,
                CreatedAt = DateTime.UtcNow
            };
            _db.BacktestResults.Add(result);
            await _db.SaveChangesAsync(ct);
            resultId = result.Id;
        }

        return new BacktestRunResult
        {
            Message = "ok",
            CandleCount = candles.Count,
            StartingCapital = capital,
            EndingCapital = finalEquity,
            TotalReturn = totalReturn,
            TotalReturnPct = totalReturnPct,
            MaxDrawdownPct = maxDrawdown,
            WinRate = winRate,
            BacktestResultsId = resultId
        };
    }

    private async Task<User> EnsureSystemUserAsync(CancellationToken ct)
    {
        var email = "system@mytrader.local";
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);
        if (user != null) return user;
        user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = "system",
            FirstName = "System",
            LastName = "User",
            CreatedAt = DateTime.UtcNow
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);
        return user;
    }

    private async Task<Strategy> EnsureStrategyAsync(Guid userId, BacktestRunRequest req, CancellationToken ct)
    {
        var name = $"AutoBacktest {req.Symbol} {req.Timeframe}";
        var st = await _db.Strategies.FirstOrDefaultAsync(s => s.UserId == userId && s.Name == name, ct);
        if (st != null) return st;
        st = new Strategy
        {
            Id = Guid.NewGuid(),
            Name = name,
            UserId = userId,
            Symbol = req.Symbol,
            Timeframe = req.Timeframe,
            IsActive = false,
            IsCustom = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Parameters = "{}",
            EntryRules = "{}",
            ExitRules = "{}"
        };
        _db.Strategies.Add(st);
        await _db.SaveChangesAsync(ct);
        return st;
    }

    // --- Indicator helpers ---
    private static List<decimal> SMA(List<decimal> values, int period)
    {
        var res = new List<decimal>(new decimal[values.Count]);
        decimal sum = 0;
        for (int i = 0; i < values.Count; i++) { sum += values[i]; if (i >= period) sum -= values[i - period]; if (i >= period - 1) res[i] = sum / period; }
        return res;
    }
    private static List<decimal> EMA(List<decimal> values, int period)
    {
        var res = new List<decimal>(new decimal[values.Count]);
        var k = 2m / (period + 1);
        decimal ema = values[0]; res[0] = ema;
        for (int i = 1; i < values.Count; i++) { ema = values[i] * k + ema * (1 - k); res[i] = ema; }
        return res;
    }
    private static List<decimal> RSI(List<decimal> values, int period)
    {
        var res = new List<decimal>(new decimal[values.Count]);
        decimal gain = 0, loss = 0;
        for (int i = 1; i < values.Count; i++)
        {
            var ch = values[i] - values[i - 1];
            gain = (gain * (period - 1) + Math.Max(ch, 0)) / period;
            loss = (loss * (period - 1) + Math.Abs(Math.Min(ch, 0))) / period;
            res[i] = loss == 0 ? 100 : 100 - (100 / (1 + (gain / (loss == 0 ? 1 : loss))));
        }
        return res;
    }
    private static (List<decimal> U, List<decimal> M, List<decimal> L) Bollinger(List<decimal> values, int period, decimal mult)
    {
        var m = SMA(values, period);
        var u = new List<decimal>(new decimal[values.Count]);
        var l = new List<decimal>(new decimal[values.Count]);
        for (int i = 0; i < values.Count; i++)
        {
            if (i < period - 1) continue;
            decimal mean = m[i];
            decimal sumSq = 0;
            for (int j = i - period + 1; j <= i; j++) sumSq += (values[j] - mean) * (values[j] - mean);
            var std = (decimal)Math.Sqrt((double)(sumSq / period));
            u[i] = mean + mult * std;
            l[i] = mean - mult * std;
        }
        return (u, m, l);
    }
}

public class BacktestRunRequest
{
    public string Symbol { get; set; } = string.Empty;
    public string? Venue { get; set; } = "BINANCE";
    public string Timeframe { get; set; } = "1h";
    public DateTime? StartUtc { get; set; }
    public DateTime? EndUtc { get; set; }
    public decimal? InitialCapital { get; set; }
    public decimal? RiskPct { get; set; }
    public int? BollingerPeriod { get; set; }
    public decimal? BollingerStdDev { get; set; }
    public int? RsiPeriod { get; set; }
    public bool Persist { get; set; } = true;
}

public class BacktestRunResult
{
    public string Message { get; set; } = "";
    public int CandleCount { get; set; }
    public decimal StartingCapital { get; set; }
    public decimal EndingCapital { get; set; }
    public decimal TotalReturn { get; set; }
    public decimal TotalReturnPct { get; set; }
    public decimal MaxDrawdownPct { get; set; }
    public decimal WinRate { get; set; }
    public Guid? BacktestResultsId { get; set; }
}
