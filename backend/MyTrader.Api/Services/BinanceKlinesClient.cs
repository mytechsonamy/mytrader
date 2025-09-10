using System.Net.Http;
using System.Text.Json;

namespace MyTrader.Api.Services;

public class BinanceKlinesClient
{
    private readonly HttpClient _http;

    public BinanceKlinesClient(HttpClient http)
    {
        _http = http;
        _http.Timeout = TimeSpan.FromSeconds(30);
    }

    public async IAsyncEnumerable<Kline> GetKlinesAsync(string symbol, string interval, DateTime startUtc, DateTime endUtc, int limit = 1000, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        // Binance expects milliseconds since epoch
        long startMs = new DateTimeOffset(startUtc).ToUnixTimeMilliseconds();
        long endMs = new DateTimeOffset(endUtc).ToUnixTimeMilliseconds();

        var current = startMs;
        while (current < endMs)
        {
            var url = $"https://api.binance.com/api/v3/klines?symbol={symbol}&interval={interval}&startTime={current}&endTime={endMs}&limit={limit}";
            var json = await _http.GetStringAsync(url, ct);
            var arr = JsonSerializer.Deserialize<JsonElement>(json);
            if (arr.ValueKind != JsonValueKind.Array || arr.GetArrayLength() == 0)
                yield break;

            var any = false;
            foreach (var item in arr.EnumerateArray())
            {
                any = true;
                var openTime = DateTimeOffset.FromUnixTimeMilliseconds(item[0].GetInt64()).UtcDateTime;
                var open = decimal.Parse(item[1].GetString() ?? "0");
                var high = decimal.Parse(item[2].GetString() ?? "0");
                var low = decimal.Parse(item[3].GetString() ?? "0");
                var close = decimal.Parse(item[4].GetString() ?? "0");
                var volume = decimal.Parse(item[5].GetString() ?? "0");
                var closeTime = DateTimeOffset.FromUnixTimeMilliseconds(item[6].GetInt64()).UtcDateTime;

                yield return new Kline
                {
                    OpenTime = openTime,
                    CloseTime = closeTime,
                    Open = open,
                    High = high,
                    Low = low,
                    Close = close,
                    Volume = volume
                };

                current = item[6].GetInt64() + 1; // advance beyond this kline
            }

            if (!any)
                yield break;

            // Be polite with API
            await Task.Delay(250, ct);
        }
    }
}

public record Kline
{
    public DateTime OpenTime { get; init; }
    public DateTime CloseTime { get; init; }
    public decimal Open { get; init; }
    public decimal High { get; init; }
    public decimal Low { get; init; }
    public decimal Close { get; init; }
    public decimal Volume { get; init; }
}

