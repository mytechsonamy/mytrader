namespace MyTrader.Core.DTOs.Dashboard;

// Dashboard Snapshot DTOs
public class DashboardSnapshot
{
    public PortfolioSummaryDto Portfolio { get; set; } = new();
    public List<SignalAlertDto> RecentSignals { get; set; } = new();
    public List<StrategyStatusDto> ActiveStrategies { get; set; } = new();
    public MarketOverviewDto MarketOverview { get; set; } = new();
    public DateTime Timestamp { get; set; }
}

// Portfolio DTOs
public class PortfolioSummaryDto
{
    public decimal TotalValue { get; set; }
    public decimal DayChange { get; set; }
    public decimal DayChangePercent { get; set; }
    public decimal TotalReturn { get; set; }
    public decimal TotalReturnPercent { get; set; }
    public int ActivePositions { get; set; }
    public decimal CashBalance { get; set; }
    public DateTime LastUpdated { get; set; }
}

public class PortfolioUpdateDto
{
    public decimal TotalValue { get; set; }
    public decimal Change { get; set; }
    public decimal ChangePercent { get; set; }
    public List<PositionUpdateDto> PositionUpdates { get; set; } = new();
    public DateTime Timestamp { get; set; }
}

public class PositionUpdateDto
{
    public string Symbol { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal AveragePrice { get; set; }
    public decimal CurrentPrice { get; set; }
    public decimal UnrealizedPnL { get; set; }
    public decimal UnrealizedPnLPercent { get; set; }
    public DateTime LastUpdated { get; set; }
}

// Signal DTOs
public class SignalAlertDto
{
    public string Symbol { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // BUY, SELL, HOLD
    public decimal Confidence { get; set; }
    public decimal Price { get; set; }
    public string Source { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string Priority { get; set; } = "Normal"; // Low, Normal, High, Critical
}

public class SignalNotification
{
    public Guid Id { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public decimal Confidence { get; set; }
    public decimal Strength { get; set; }
    public decimal Price { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
    public bool IsRealTime { get; set; } = true;
    public Dictionary<string, object> Metadata { get; set; } = new();
}

// Strategy DTOs
public class StrategyStatusDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty; // Active, Paused, Stopped
    public decimal Performance { get; set; } // % return
    public DateTime LastSignal { get; set; }
    public int OpenPositions { get; set; }
    public decimal DailyPnL { get; set; }
}

public class StrategyPerformanceUpdate
{
    public Guid StrategyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal TotalReturn { get; set; }
    public decimal DailyReturn { get; set; }
    public int TotalTrades { get; set; }
    public decimal WinRate { get; set; }
    public decimal MaxDrawdown { get; set; }
    public DateTime LastUpdated { get; set; }
}

// Market Overview DTOs
public class MarketOverviewDto
{
    public string MarketSentiment { get; set; } = string.Empty; // Bullish, Bearish, Neutral
    public decimal VolumeIndex { get; set; } // Market volume index
    public decimal VolatilityIndex { get; set; } // VIX-like indicator
    public List<MarketMoverDto> TopMovers { get; set; } = new();
    public List<MarketEventDto> UpcomingEvents { get; set; } = new();
    public DateTime LastUpdated { get; set; }
}

public class MarketMoverDto
{
    public string Symbol { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal Change { get; set; }
    public decimal ChangePercent { get; set; }
    public decimal Volume { get; set; }
    public string Direction { get; set; } = string.Empty; // Up, Down
}

public class MarketEventDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string Type { get; set; } = string.Empty; // earnings, economic, news
    public string Impact { get; set; } = string.Empty; // High, Medium, Low
    public List<string> AffectedSymbols { get; set; } = new();
}

// Real-time Update DTOs
public class RealTimeUpdate
{
    public string Type { get; set; } = string.Empty; // price, indicator, signal, portfolio
    public string Symbol { get; set; } = string.Empty;
    public Dictionary<string, object> Data { get; set; } = new();
    public DateTime Timestamp { get; set; }
    public string Source { get; set; } = string.Empty;
}

public class PriceUpdateDto
{
    public string Symbol { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal Change { get; set; }
    public decimal ChangePercent { get; set; }
    public decimal Volume { get; set; }
    public decimal High24h { get; set; }
    public decimal Low24h { get; set; }
    public DateTime Timestamp { get; set; }
}

// Notification DTOs
public class NotificationPreferences
{
    public bool EnableSignalAlerts { get; set; } = true;
    public bool EnablePortfolioAlerts { get; set; } = true;
    public bool EnableStrategyAlerts { get; set; } = true;
    public bool EnableMarketAlerts { get; set; } = false;
    public decimal MinSignalConfidence { get; set; } = 70m;
    public decimal PortfolioChangeThreshold { get; set; } = 5m; // %
    public List<string> AlertMethods { get; set; } = new(); // push, email, sms
    public QuietHours QuietHours { get; set; } = new();
}

public class QuietHours
{
    public bool Enabled { get; set; } = false;
    public TimeSpan StartTime { get; set; } = new(22, 0, 0); // 10 PM
    public TimeSpan EndTime { get; set; } = new(8, 0, 0);   // 8 AM
    public List<DayOfWeek> Days { get; set; } = new() { DayOfWeek.Saturday, DayOfWeek.Sunday };
}

// Chart and Visualization DTOs
public class ChartDataDto
{
    public string Symbol { get; set; } = string.Empty;
    public string Timeframe { get; set; } = string.Empty;
    public List<CandleDto> Candles { get; set; } = new();
    public List<IndicatorLineDto> Indicators { get; set; } = new();
    public List<SignalMarkerDto> Signals { get; set; } = new();
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
}

public class CandleDto
{
    public DateTime Timestamp { get; set; }
    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Close { get; set; }
    public decimal Volume { get; set; }
}

public class IndicatorLineDto
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // line, area, histogram
    public string Color { get; set; } = string.Empty;
    public List<DataPointDto> Data { get; set; } = new();
    public bool IsVisible { get; set; } = true;
}

public class DataPointDto
{
    public DateTime Timestamp { get; set; }
    public decimal Value { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}

public class SignalMarkerDto
{
    public DateTime Timestamp { get; set; }
    public decimal Price { get; set; }
    public string Type { get; set; } = string.Empty; // BUY, SELL
    public string Source { get; set; } = string.Empty;
    public decimal Confidence { get; set; }
    public string Color { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
}

// Performance Tracking DTOs
public class PerformanceSnapshotDto
{
    public Guid UserId { get; set; }
    public decimal TotalPortfolioValue { get; set; }
    public decimal DayChange { get; set; }
    public decimal WeekChange { get; set; }
    public decimal MonthChange { get; set; }
    public decimal YearChange { get; set; }
    public decimal AllTimeReturn { get; set; }
    public List<PerformanceMetricDto> Metrics { get; set; } = new();
    public DateTime CalculatedAt { get; set; }
}

public class PerformanceMetricDto
{
    public string Name { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public string Unit { get; set; } = string.Empty; // %, $, ratio
    public string Category { get; set; } = string.Empty; // return, risk, efficiency
    public string Description { get; set; } = string.Empty;
}

// Mobile-specific DTOs
public class MobileDashboardDto
{
    public QuickStatsDto QuickStats { get; set; } = new();
    public List<WatchlistItemDto> Watchlist { get; set; } = new();
    public List<SignalAlertDto> TopSignals { get; set; } = new();
    public List<StrategyCardDto> ActiveStrategies { get; set; } = new();
    public MarketBriefDto MarketBrief { get; set; } = new();
    public DateTime LastUpdated { get; set; }
}

public class QuickStatsDto
{
    public decimal PortfolioValue { get; set; }
    public decimal DayChange { get; set; }
    public decimal DayChangePercent { get; set; }
    public string Trend { get; set; } = string.Empty; // up, down, flat
    public int ActivePositions { get; set; }
    public int NewSignals { get; set; }
}

public class WatchlistItemDto
{
    public string Symbol { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal Change { get; set; }
    public decimal ChangePercent { get; set; }
    public string Trend { get; set; } = string.Empty;
    public List<string> ActiveSignals { get; set; } = new();
    public MiniChartDto Chart { get; set; } = new();
}

public class MiniChartDto
{
    public List<decimal> Prices { get; set; } = new();
    public string Color { get; set; } = string.Empty;
    public string Period { get; set; } = string.Empty; // 24h, 7d, 30d
}

public class StrategyCardDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Performance { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime LastSignal { get; set; }
    public string TrendIndicator { get; set; } = string.Empty; // ↗️, ↘️, ➡️
}

public class MarketBriefDto
{
    public string OverallSentiment { get; set; } = string.Empty;
    public List<MarketMoverDto> TopMovers { get; set; } = new();
    public List<string> KeyEvents { get; set; } = new();
    public decimal VolumeIndex { get; set; }
}

// Error and Status DTOs
public class ConnectionStatusDto
{
    public bool IsConnected { get; set; }
    public DateTime ConnectedAt { get; set; }
    public List<string> ActiveSubscriptions { get; set; } = new();
    public DateTime LastHeartbeat { get; set; }
    public int ReconnectAttempts { get; set; }
}

public class ErrorMessageDto
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string Severity { get; set; } = "Error"; // Info, Warning, Error, Critical
}

// Subscription DTOs
public class SubscriptionRequest
{
    public string Type { get; set; } = string.Empty; // indicators, signals, portfolio, strategies
    public Dictionary<string, object> Parameters { get; set; } = new();
}

public class SubscriptionConfirmation
{
    public string Type { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public Dictionary<string, object> Details { get; set; } = new();
    public DateTime Timestamp { get; set; }
}