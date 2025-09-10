using MyTrader.Core.DTOs.Analytics;

namespace MyTrader.Services.Analytics;

public interface IAnalyticsService
{
    // Performance Analytics
    Task<PerformanceAnalytics> GetPerformanceAnalyticsAsync(Guid userId, string period = "30D");
    Task<PerformanceAnalytics> GetStrategyPerformanceAsync(Guid userId, Guid strategyId, string period = "30D");
    Task<List<StrategyAnalytics>> GetTopPerformingStrategiesAsync(Guid userId, int limit = 10);
    Task<List<SymbolAnalytics>> GetSymbolPerformanceAsync(Guid userId, string period = "30D");

    // Chart Data Generation
    Task<EquityCurveData> GetEquityCurveAsync(Guid userId, string period = "30D");
    Task<ChartDataResponse> GetPnLChartAsync(Guid userId, string period = "30D", string interval = "1D");
    Task<PnLDistributionData> GetPnLDistributionAsync(Guid userId, string period = "30D");
    Task<ChartDataResponse> GetWinRateChartAsync(Guid userId, string period = "30D");
    Task<HeatmapData> GetStrategyHeatmapAsync(Guid userId);
    Task<ChartDataResponse> GetVolumeAnalysisAsync(Guid userId, string period = "30D");
    Task<ChartDataResponse> GetDrawdownAnalysisAsync(Guid userId, string period = "30D");

    // Risk Analytics
    Task<RiskMetrics> GetRiskAnalyticsAsync(Guid userId, string period = "30D");
    Task<ChartDataResponse> GetVolatilityAnalysisAsync(Guid userId, string period = "30D");
    Task<ChartDataResponse> GetCorrelationAnalysisAsync(Guid userId, List<string> symbols);

    // Comparative Analytics
    Task<ChartDataResponse> GetBenchmarkComparisonAsync(Guid userId, string benchmark = "SPY", string period = "30D");
    Task<List<PerformanceAnalytics>> GetPeerComparisonAsync(Guid userId, int peerCount = 10);

    // Real-time Analytics
    Task<Dictionary<string, decimal>> GetRealTimeMetricsAsync(Guid userId);
    Task<List<ChartDataPoint>> GetRecentTradeActivityAsync(Guid userId, int hours = 24);

    // Export Functions
    Task<byte[]> ExportAnalyticsReportAsync(Guid userId, string period = "30D", string format = "PDF");
    Task<string> GenerateAnalyticsSummaryAsync(Guid userId, string period = "30D");
}