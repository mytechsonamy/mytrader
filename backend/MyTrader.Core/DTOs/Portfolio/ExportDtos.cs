namespace MyTrader.Core.DTOs.Portfolio;

public class ExportRequestDto
{
    public Guid PortfolioId { get; set; }
    public string ExportType { get; set; } = "CSV"; // CSV, PDF, EXCEL
    public string ReportType { get; set; } = "Portfolio"; // Portfolio, Transactions, Performance, Analytics
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public bool IncludePositions { get; set; } = true;
    public bool IncludeTransactions { get; set; } = true;
    public bool IncludePerformance { get; set; } = true;
    public bool IncludeRiskMetrics { get; set; } = true;
    public string? Currency { get; set; } = "USD";
}

public class ExportResponseDto
{
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public byte[] FileContent { get; set; } = Array.Empty<byte>();
    public long FileSizeBytes { get; set; }
    public DateTime GeneratedAt { get; set; }
    public string DownloadUrl { get; set; } = string.Empty;
}

public class PortfolioReportDto
{
    public Guid PortfolioId { get; set; }
    public string PortfolioName { get; set; } = string.Empty;
    public string BaseCurrency { get; set; } = string.Empty;
    public DateTime ReportDate { get; set; }
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    
    // Summary Section
    public PortfolioSummarySection Summary { get; set; } = new();
    
    // Performance Section
    public PerformanceReportSection Performance { get; set; } = new();
    
    // Risk Section
    public RiskReportSection Risk { get; set; } = new();
    
    // Holdings Section
    public HoldingsReportSection Holdings { get; set; } = new();
    
    // Transactions Section
    public TransactionsReportSection Transactions { get; set; } = new();
    
    // Charts and Visualizations
    public ReportChartsSection Charts { get; set; } = new();
}

public class PortfolioSummarySection
{
    public decimal InitialCapital { get; set; }
    public decimal CurrentValue { get; set; }
    public decimal CashBalance { get; set; }
    public decimal TotalPnL { get; set; }
    public decimal TotalPnLPercent { get; set; }
    public decimal DailyPnL { get; set; }
    public decimal WeeklyPnL { get; set; }
    public decimal MonthlyPnL { get; set; }
    public decimal YearToDatePnL { get; set; }
    public int TotalPositions { get; set; }
    public int TotalTransactions { get; set; }
    public DateTime LastUpdated { get; set; }
}

public class PerformanceReportSection
{
    public decimal TotalReturn { get; set; }
    public decimal AnnualizedReturn { get; set; }
    public decimal BestDay { get; set; }
    public decimal WorstDay { get; set; }
    public decimal WinRate { get; set; }
    public decimal ProfitFactor { get; set; }
    public int WinningDays { get; set; }
    public int LosingDays { get; set; }
    public List<MonthlyPerformanceDto> MonthlyBreakdown { get; set; } = new();
}

public class RiskReportSection
{
    public decimal Volatility { get; set; }
    public decimal SharpeRatio { get; set; }
    public decimal SortinoRatio { get; set; }
    public decimal MaxDrawdown { get; set; }
    public decimal MaxDrawdownPercent { get; set; }
    public decimal CurrentDrawdown { get; set; }
    public decimal ValueAtRisk { get; set; }
    public decimal Beta { get; set; }
    public decimal Alpha { get; set; }
}

public class HoldingsReportSection
{
    public List<PositionReportDto> Positions { get; set; } = new();
    public List<AllocationDto> AssetAllocation { get; set; } = new();
    public List<AllocationDto> SectorAllocation { get; set; } = new();
    public decimal TotalMarketValue { get; set; }
    public int TotalHoldings { get; set; }
}

public class TransactionsReportSection
{
    public List<TransactionDto> RecentTransactions { get; set; } = new();
    public TransactionSummaryDto Summary { get; set; } = new();
    public int TotalTransactionsInPeriod { get; set; }
    public decimal TotalVolumeTraded { get; set; }
    public decimal TotalFeesPaid { get; set; }
}

public class ReportChartsSection
{
    public List<ChartDataPoint> PortfolioValueChart { get; set; } = new();
    public List<ChartDataPoint> PerformanceChart { get; set; } = new();
    public List<ChartDataPoint> DrawdownChart { get; set; } = new();
    public List<PieChartData> AllocationChart { get; set; } = new();
}

public class MonthlyPerformanceDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public string MonthName { get; set; } = string.Empty;
    public decimal Return { get; set; }
    public decimal ReturnPercent { get; set; }
    public decimal StartValue { get; set; }
    public decimal EndValue { get; set; }
    public int TradingDays { get; set; }
}

public class PositionReportDto
{
    public string Symbol { get; set; } = string.Empty;
    public string SymbolName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal AveragePrice { get; set; }
    public decimal CurrentPrice { get; set; }
    public decimal MarketValue { get; set; }
    public decimal UnrealizedPnL { get; set; }
    public decimal UnrealizedPnLPercent { get; set; }
    public decimal Weight { get; set; }
    public DateTime FirstPurchaseDate { get; set; }
    public DateTime LastTradeDate { get; set; }
}

public class TransactionSummaryDto
{
    public int TotalBuyTransactions { get; set; }
    public int TotalSellTransactions { get; set; }
    public decimal TotalBuyVolume { get; set; }
    public decimal TotalSellVolume { get; set; }
    public decimal AverageBuyPrice { get; set; }
    public decimal AverageSellPrice { get; set; }
    public decimal TotalFees { get; set; }
    public string MostTradedSymbol { get; set; } = string.Empty;
    public int UniqueTradedSymbols { get; set; }
}

public class ChartDataPoint
{
    public DateTime Date { get; set; }
    public decimal Value { get; set; }
    public string Label { get; set; } = string.Empty;
}

public class PieChartData
{
    public string Label { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public decimal Percentage { get; set; }
    public string Color { get; set; } = string.Empty;
}

public class CsvExportDto
{
    public string FileName { get; set; } = string.Empty;
    public List<Dictionary<string, object>> Data { get; set; } = new();
    public List<string> Headers { get; set; } = new();
}

public class PdfExportDto
{
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public PortfolioReportDto ReportData { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
}