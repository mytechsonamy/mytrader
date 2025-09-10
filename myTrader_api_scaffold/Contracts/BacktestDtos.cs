using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace MyTrader.Contracts;

public record BacktestRequest(
    [Required] string Symbol,
    [Required, MaxLength(10)] string Timeframe,
    DateTimeOffset DateRangeStart,
    DateTimeOffset DateRangeEnd
);

public record BacktestResponse(Guid BacktestId, string Status);

public record BacktestResult(
    Guid Id,
    string Status,
    DateTimeOffset StartedAt,
    DateTimeOffset? FinishedAt,
    decimal? TotalReturn,
    decimal? SharpeRatio,
    decimal? MaxDrawdown,
    int? TotalTrades
);
