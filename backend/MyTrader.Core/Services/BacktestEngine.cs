using Microsoft.Extensions.Logging;
using MyTrader.Core.Models;
using MyTrader.Core.DTOs.Indicators;

namespace MyTrader.Core.Services;

public interface IBacktestEngine
{
    Task<BacktestResults> RunBacktestAsync(BacktestRequest request);
    Task<List<BacktestResults>> RunOptimizationAsync(OptimizationRequest request);
    Task<BacktestResults> GetBacktestResultsAsync(Guid backtestId);
}

public class BacktestEngine : IBacktestEngine
{
    private readonly ILogger<BacktestEngine> _logger;
    private readonly IIndicatorCalculator _indicatorCalculator;
    private readonly IMarketDataService _marketDataService;

    public BacktestEngine(
        ILogger<BacktestEngine> logger,
        IIndicatorCalculator indicatorCalculator,
        IMarketDataService marketDataService)
    {
        _logger = logger;
        _indicatorCalculator = indicatorCalculator;
        _marketDataService = marketDataService;
    }

    public async Task<BacktestResults> RunBacktestAsync(BacktestRequest request)
    {
        _logger.LogInformation("Starting backtest for strategy {StrategyId} on symbol {SymbolId}", 
            request.StrategyId, request.SymbolId);

        var marketData = await _marketDataService.GetHistoricalDataAsync(
            request.SymbolId, request.StartDate, request.EndDate, request.Timeframe);

        if (marketData.Count < 50)
        {
            throw new InvalidOperationException("Insufficient market data for backtest");
        }

        var backtestResults = new BacktestResults
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            StrategyId = request.StrategyId,
            SymbolId = request.SymbolId,
            ConfigurationId = request.ConfigurationId,
            Status = "Running",
            Timeframe = request.Timeframe,
            StartingCapital = request.InitialBalance,
            CreatedAt = DateTime.UtcNow
        };

        try
        {
            // Initialize portfolio
            var portfolio = new PortfolioState
            {
                Cash = request.InitialBalance,
                Position = 0m,
                EntryPrice = 0m,
                MaxDrawdown = 0m,
                Peak = request.InitialBalance
            };

            var trades = new List<TradeExecution>();
            var signals = await GenerateSignalsAsync(marketData, request.StrategyParameters);

            // Execute backtest
            foreach (var signal in signals.OrderBy(s => s.Timestamp))
            {
                var candle = marketData.FirstOrDefault(m => m.Timestamp == signal.Timestamp);
                if (candle == null) continue;

                var trade = ExecuteTrade(signal, candle, ref portfolio);
                if (trade != null)
                {
                    trades.Add(trade);
                }

                // Update drawdown tracking
                var currentValue = portfolio.Cash + (portfolio.Position * candle.Close);
                if (currentValue > portfolio.Peak)
                {
                    portfolio.Peak = currentValue;
                }
                else
                {
                    var drawdown = (portfolio.Peak - currentValue) / portfolio.Peak;
                    portfolio.MaxDrawdown = Math.Max(portfolio.MaxDrawdown, drawdown);
                }
            }

            // Calculate final metrics
            var finalValue = portfolio.Cash + (portfolio.Position * marketData.Last().Close);
            backtestResults.EndingCapital = finalValue;
            backtestResults.TotalReturn = finalValue - request.InitialBalance;
            backtestResults.TotalReturnPercentage = (backtestResults.TotalReturn / request.InitialBalance) * 100;
            backtestResults.MaxDrawdown = portfolio.MaxDrawdown;
            backtestResults.MaxDrawdownPercentage = portfolio.MaxDrawdown * 100;

            // Calculate trade statistics
            var winningTrades = trades.Where(t => t.PnL > 0).ToList();
            var losingTrades = trades.Where(t => t.PnL < 0).ToList();
            
            backtestResults.TotalTrades = trades.Count;
            backtestResults.WinningTrades = winningTrades.Count;
            backtestResults.LosingTrades = losingTrades.Count;
            backtestResults.WinRate = trades.Count > 0 ? (decimal)winningTrades.Count / trades.Count * 100 : 0;

            // Calculate Sharpe ratio
            if (trades.Count > 1)
            {
                var returns = trades.Select(t => t.PnL / request.InitialBalance).ToList();
                var avgReturn = returns.Average();
                var stdDev = Math.Sqrt(returns.Select(r => Math.Pow((double)(r - (decimal)avgReturn), 2)).Average());
                backtestResults.SharpeRatio = stdDev > 0 ? (decimal)(avgReturn / (decimal)stdDev) * (decimal)Math.Sqrt(252) : 0;
            }

            // Calculate annualized return
            var daysDiff = (request.EndDate - request.StartDate).TotalDays;
            if (daysDiff > 0)
            {
                var annualizedReturn = Math.Pow((double)(finalValue / request.InitialBalance), 365.0 / daysDiff) - 1;
                backtestResults.AnnualizedReturn = (decimal)annualizedReturn * 100;
            }

            backtestResults.Status = "Completed";
            backtestResults.DetailedResults = System.Text.Json.JsonSerializer.Serialize(new
            {
                Trades = trades,
                FinalPortfolio = portfolio,
                Metrics = new
                {
                    backtestResults.TotalReturn,
                    backtestResults.TotalReturnPercentage,
                    backtestResults.MaxDrawdown,
                    backtestResults.SharpeRatio,
                    backtestResults.WinRate
                }
            });

            _logger.LogInformation("Backtest completed successfully. Total return: {TotalReturn:C}, Win rate: {WinRate:F1}%", 
                backtestResults.TotalReturn, backtestResults.WinRate);

            return backtestResults;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during backtest execution");
            backtestResults.Status = "Failed";
            throw;
        }
    }

    public async Task<List<BacktestResults>> RunOptimizationAsync(OptimizationRequest request)
    {
        _logger.LogInformation("Starting parameter optimization for strategy {StrategyId}", request.StrategyId);

        var results = new List<BacktestResults>();
        var parameterCombinations = GenerateParameterCombinations(request.ParameterRanges);

        var tasks = parameterCombinations.Select(async parameters =>
        {
            var backtestRequest = new BacktestRequest
            {
                UserId = request.UserId,
                StrategyId = request.StrategyId,
                SymbolId = request.SymbolId,
                ConfigurationId = request.ConfigurationId,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                Timeframe = request.Timeframe,
                InitialBalance = request.InitialBalance,
                StrategyParameters = parameters
            };

            try
            {
                return await RunBacktestAsync(backtestRequest);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to run backtest with parameters {Parameters}", 
                    System.Text.Json.JsonSerializer.Serialize(parameters));
                return null;
            }
        });

        var backtestResults = await Task.WhenAll(tasks);
        results.AddRange(backtestResults.Where(r => r != null));

        _logger.LogInformation("Optimization completed. Generated {Count} results", results.Count);
        return results.OrderByDescending(r => r.SharpeRatio).ToList();
    }

    public async Task<BacktestResults> GetBacktestResultsAsync(Guid backtestId)
    {
        // This would typically fetch from database
        throw new NotImplementedException("Database integration required");
    }

    private async Task<List<TradingSignal>> GenerateSignalsAsync(List<MarketData> marketData, StrategyParameters parameters)
    {
        var signals = new List<TradingSignal>();

        for (int i = Math.Max(parameters.LookbackPeriod, 50); i < marketData.Count; i++)
        {
            var currentData = marketData.Take(i + 1).ToList();
            var candle = marketData[i];

            // Calculate indicators
            var rsi = await _indicatorCalculator.CalculateRSIAsync(currentData, parameters.RSIPeriod);
            var macd = await _indicatorCalculator.CalculateMACDAsync(currentData, 
                parameters.MACDFast, parameters.MACDSlow, parameters.MACDSignal);
            var bb = await _indicatorCalculator.CalculateBollingerBandsAsync(currentData, 
                parameters.BBPeriod, parameters.BBStdDev);

            // Generate signals based on strategy logic
            var signal = EvaluateStrategy(candle, rsi, macd, bb, parameters);
            if (signal != null)
            {
                signal.Timestamp = candle.Timestamp;
                signals.Add(signal);
            }
        }

        return signals;
    }

    private TradingSignal? EvaluateStrategy(MarketData candle, RSI rsi, MACD macd, BollingerBands bb, StrategyParameters parameters)
    {
        // Multi-indicator strategy logic
        var buySignals = 0;
        var sellSignals = 0;

        // RSI signals
        if (rsi.Value < parameters.RSIOversold) buySignals++;
        if (rsi.Value > parameters.RSIOverbought) sellSignals++;

        // MACD signals
        if (macd.MACDLine > macd.SignalLine && macd.Histogram > 0) buySignals++;
        if (macd.MACDLine < macd.SignalLine && macd.Histogram < 0) sellSignals++;

        // Bollinger Bands signals
        if (candle.Close < bb.LowerBand) buySignals++;
        if (candle.Close > bb.UpperBand) sellSignals++;

        // Generate signal based on threshold
        if (buySignals >= parameters.SignalThreshold)
        {
            return new TradingSignal
            {
                Type = "BUY",
                Strength = (decimal)buySignals / 3,
                Price = candle.Close,
                Indicators = new Dictionary<string, decimal>
                {
                    ["RSI"] = rsi.Value,
                    ["MACD"] = macd.MACDLine,
                    ["BB_Position"] = (candle.Close - bb.LowerBand) / (bb.UpperBand - bb.LowerBand)
                }
            };
        }
        else if (sellSignals >= parameters.SignalThreshold)
        {
            return new TradingSignal
            {
                Type = "SELL",
                Strength = (decimal)sellSignals / 3,
                Price = candle.Close,
                Indicators = new Dictionary<string, decimal>
                {
                    ["RSI"] = rsi.Value,
                    ["MACD"] = macd.MACDLine,
                    ["BB_Position"] = (candle.Close - bb.LowerBand) / (bb.UpperBand - bb.LowerBand)
                }
            };
        }

        return null;
    }

    private TradeExecution? ExecuteTrade(TradingSignal signal, MarketData candle, ref PortfolioState portfolio)
    {
        if (signal.Type == "BUY" && portfolio.Position == 0)
        {
            // Open long position
            var quantity = portfolio.Cash / candle.Close * 0.95m; // 95% of available cash
            portfolio.Position = quantity;
            portfolio.Cash -= quantity * candle.Close;
            portfolio.EntryPrice = candle.Close;

            return new TradeExecution
            {
                Id = Guid.NewGuid(),
                Timestamp = signal.Timestamp,
                Type = "BUY",
                Quantity = quantity,
                Price = candle.Close,
                PnL = 0
            };
        }
        else if (signal.Type == "SELL" && portfolio.Position > 0)
        {
            // Close long position
            var sellValue = portfolio.Position * candle.Close;
            var pnl = sellValue - (portfolio.Position * portfolio.EntryPrice);
            
            portfolio.Cash += sellValue;
            var trade = new TradeExecution
            {
                Id = Guid.NewGuid(),
                Timestamp = signal.Timestamp,
                Type = "SELL",
                Quantity = portfolio.Position,
                Price = candle.Close,
                PnL = pnl
            };

            portfolio.Position = 0;
            portfolio.EntryPrice = 0;

            return trade;
        }

        return null;
    }

    private List<StrategyParameters> GenerateParameterCombinations(Dictionary<string, ParameterRange> ranges)
    {
        var combinations = new List<StrategyParameters>();

        // Generate all combinations of parameters within ranges
        var rsiPeriods = GenerateRange(ranges["RSIPeriod"]);
        var macdFasts = GenerateRange(ranges["MACDFast"]);
        var macdSlows = GenerateRange(ranges["MACDSlow"]);
        var bbPeriods = GenerateRange(ranges["BBPeriod"]);

        foreach (var rsiPeriod in rsiPeriods)
        foreach (var macdFast in macdFasts)
        foreach (var macdSlow in macdSlows.Where(s => s > macdFast))
        foreach (var bbPeriod in bbPeriods)
        {
            combinations.Add(new StrategyParameters
            {
                RSIPeriod = rsiPeriod,
                MACDFast = macdFast,
                MACDSlow = macdSlow,
                MACDSignal = 9, // Fixed signal period
                BBPeriod = bbPeriod,
                BBStdDev = 2.0m, // Fixed std dev
                RSIOversold = 30,
                RSIOverbought = 70,
                SignalThreshold = 2,
                LookbackPeriod = Math.Max(rsiPeriod, Math.Max(macdSlow, bbPeriod))
            });
        }

        return combinations;
    }

    private List<int> GenerateRange(ParameterRange range)
    {
        var values = new List<int>();
        for (int i = range.Min; i <= range.Max; i += range.Step)
        {
            values.Add(i);
        }
        return values;
    }
}

// Supporting classes
public class BacktestRequest
{
    public Guid UserId { get; set; }
    public Guid StrategyId { get; set; }
    public Guid SymbolId { get; set; }
    public Guid ConfigurationId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Timeframe { get; set; } = string.Empty;
    public decimal InitialBalance { get; set; } = 10000m;
    public StrategyParameters StrategyParameters { get; set; } = new();
}

public class OptimizationRequest : BacktestRequest
{
    public Dictionary<string, ParameterRange> ParameterRanges { get; set; } = new();
}

public class ParameterRange
{
    public int Min { get; set; }
    public int Max { get; set; }
    public int Step { get; set; } = 1;
}

public class StrategyParameters
{
    public int RSIPeriod { get; set; } = 14;
    public int MACDFast { get; set; } = 12;
    public int MACDSlow { get; set; } = 26;
    public int MACDSignal { get; set; } = 9;
    public int BBPeriod { get; set; } = 20;
    public decimal BBStdDev { get; set; } = 2.0m;
    public decimal RSIOversold { get; set; } = 30;
    public decimal RSIOverbought { get; set; } = 70;
    public int SignalThreshold { get; set; } = 2;
    public int LookbackPeriod { get; set; } = 50;
}

public class PortfolioState
{
    public decimal Cash { get; set; }
    public decimal Position { get; set; }
    public decimal EntryPrice { get; set; }
    public decimal MaxDrawdown { get; set; }
    public decimal Peak { get; set; }
}

public class TradingSignal
{
    public string Type { get; set; } = string.Empty;
    public decimal Strength { get; set; }
    public decimal Price { get; set; }
    public DateTime Timestamp { get; set; }
    public Dictionary<string, decimal> Indicators { get; set; } = new();
}

public class TradeExecution
{
    public Guid Id { get; set; }
    public DateTime Timestamp { get; set; }
    public string Type { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal Price { get; set; }
    public decimal PnL { get; set; }
}