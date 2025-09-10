using System;

namespace MyTrader.Contracts;

public record CandleDto(DateTimeOffset Ts, decimal Open, decimal High, decimal Low, decimal Close, decimal Volume);
