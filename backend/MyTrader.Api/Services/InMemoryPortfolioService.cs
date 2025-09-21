using MyTrader.Core.DTOs.Portfolio;
using MyTrader.Core.Interfaces;

namespace MyTrader.API.Services;

public class InMemoryPortfolioService : IPortfolioService
{
    public async Task<PortfolioSummaryDto?> GetPortfolioAsync(Guid userId, Guid? portfolioId = null)
    {
        await Task.Delay(1); // Simulate async
        return new PortfolioSummaryDto
        {
            Id = Guid.NewGuid(),
            Name = "Test Portfolio",
            BaseCurrency = "USD",
            InitialCapital = 10000m,
            CurrentValue = 10500m,
            CashBalance = 2000m,
            TotalPnL = 500m,
            DailyPnL = 100m,
            TotalReturnPercent = 5.0m,
            LastUpdated = DateTime.UtcNow,
            Positions = new List<PortfolioPositionDto>
            {
                new PortfolioPositionDto
                {
                    Id = Guid.NewGuid(),
                    Symbol = "BTCUSDT",
                    SymbolName = "Bitcoin",
                    Quantity = 0.1m,
                    AveragePrice = 60000m,
                    CurrentPrice = 65000m,
                    MarketValue = 6500m,
                    UnrealizedPnL = 500m,
                    UnrealizedPnLPercent = 8.33m,
                    CostBasis = 6000m,
                    Weight = 61.9m,
                    LastTradedAt = DateTime.UtcNow.AddDays(-1)
                }
            }
        };
    }

    public async Task<List<PortfolioSummaryDto>> GetUserPortfoliosAsync(Guid userId)
    {
        await Task.Delay(1);
        var portfolio = await GetPortfolioAsync(userId);
        return portfolio != null ? new List<PortfolioSummaryDto> { portfolio } : new List<PortfolioSummaryDto>();
    }

    public async Task<PortfolioSummaryDto> CreatePortfolioAsync(Guid userId, CreatePortfolioDto createDto)
    {
        await Task.Delay(1);
        return new PortfolioSummaryDto
        {
            Id = Guid.NewGuid(),
            Name = createDto.Name,
            BaseCurrency = createDto.BaseCurrency,
            InitialCapital = createDto.InitialCapital,
            CurrentValue = createDto.InitialCapital,
            CashBalance = createDto.InitialCapital,
            TotalPnL = 0m,
            DailyPnL = 0m,
            TotalReturnPercent = 0m,
            LastUpdated = DateTime.UtcNow,
            Positions = new List<PortfolioPositionDto>()
        };
    }

        public Task<PortfolioAnalyticsDto> GetPortfolioAnalyticsAsync(Guid userId, AnalyticsRequestDto request)
        => Task.FromResult(new PortfolioAnalyticsDto
        {
            PortfolioId = request.PortfolioId,
            PortfolioName = "Test Portfolio",
            AnalysisDate = DateTime.UtcNow,
            Performance = new PerformanceMetricsDto
            {
                TotalReturn = 500m,
                TotalReturnPercent = 5.0m,
                AnnualizedReturn = 12.5m,
                DailyReturn = 0.25m,
                WeeklyReturn = 1.75m,
                MonthlyReturn = 2.5m,
                YearToDateReturn = 5.0m,
                BestDay = 3.5m,
                WorstDay = -2.1m,
                WinningDays = 15,
                LosingDays = 10,
                WinRate = 60.0m,
                ProfitFactor = 1.5m
            },
            Risk = new RiskMetricsDto
            {
                Volatility = 15.2m,
                AnnualizedVolatility = 18.5m,
                SharpeRatio = 0.68m,
                SortinoRatio = 0.95m,
                MaxDrawdown = 125m,
                MaxDrawdownPercent = 1.25m,
                MaxDrawdownDate = DateTime.UtcNow.AddDays(-15),
                MaxDrawdownDuration = 5,
                CurrentDrawdown = 0m,
                ValueAtRisk = 200m,
                Beta = 0.85m,
                Alpha = 2.5m,
                InformationRatio = 0.45m
            },
            AssetAllocation = new List<AllocationDto>
            {
                new AllocationDto { Name = "Bitcoin", Category = "Cryptocurrency", Value = 6500m, Percentage = 61.9m, Weight = 0.619m, Color = "#F7931A" },
                new AllocationDto { Name = "Cash", Category = "Cash", Value = 2000m, Percentage = 19.05m, Weight = 0.1905m, Color = "#28A745" },
                new AllocationDto { Name = "Others", Category = "Mixed", Value = 2000m, Percentage = 19.05m, Weight = 0.1905m, Color = "#6C757D" }
            },
            SectorAllocation = new List<AllocationDto>
            {
                new AllocationDto { Name = "Technology", Category = "Sector", Value = 6500m, Percentage = 61.9m, Weight = 0.619m, Color = "#007BFF" },
                new AllocationDto { Name = "Finance", Category = "Sector", Value = 2000m, Percentage = 19.05m, Weight = 0.1905m, Color = "#28A745" },
                new AllocationDto { Name = "Cash", Category = "Sector", Value = 2000m, Percentage = 19.05m, Weight = 0.1905m, Color = "#FFC107" }
            },
            PerformanceHistory = Enumerable.Range(1, 30).Select(i => new PerformanceHistoryDto
            {
                Date = DateTime.UtcNow.AddDays(-i),
                PortfolioValue = 10000m + (i * 10m) + (decimal)(new Random().NextDouble() * 200 - 100),
                DailyReturn = (decimal)(new Random().NextDouble() * 4 - 2),
                CumulativeReturn = (decimal)(i * 0.5),
                Benchmark = (decimal)(i * 0.3),
                Drawdown = Math.Max(0, (decimal)(new Random().NextDouble() * 50))
            }).ToList()
        });

    public Task<PerformanceMetricsDto> GetPerformanceMetricsAsync(Guid userId, Guid portfolioId, DateTime? fromDate = null, DateTime? toDate = null)
        => Task.FromResult(new PerformanceMetricsDto
        {
            TotalReturn = 500m,
            TotalReturnPercent = 5.0m,
            AnnualizedReturn = 12.5m,
            DailyReturn = 0.25m,
            WeeklyReturn = 1.75m,
            MonthlyReturn = 2.5m,
            YearToDateReturn = 5.0m,
            BestDay = 3.5m,
            WorstDay = -2.1m,
            WinningDays = 15,
            LosingDays = 10,
            WinRate = 60.0m,
            ProfitFactor = 1.5m
        });

    public Task<RiskMetricsDto> GetRiskMetricsAsync(Guid userId, Guid portfolioId, DateTime? fromDate = null, DateTime? toDate = null)
        => Task.FromResult(new RiskMetricsDto
        {
            Volatility = 15.2m,
            AnnualizedVolatility = 18.5m,
            SharpeRatio = 0.68m,
            SortinoRatio = 0.95m,
            MaxDrawdown = 125m,
            MaxDrawdownPercent = 1.25m,
            MaxDrawdownDate = DateTime.UtcNow.AddDays(-15),
            MaxDrawdownDuration = 5,
            CurrentDrawdown = 0m,
            ValueAtRisk = 200m,
            Beta = 0.85m,
            Alpha = 2.5m,
            InformationRatio = 0.45m
        });

    public Task<List<AllocationDto>> GetAssetAllocationDetailAsync(Guid userId, Guid portfolioId)
        => Task.FromResult(new List<AllocationDto>
        {
            new AllocationDto { Name = "Bitcoin", Category = "Cryptocurrency", Value = 6500m, Percentage = 61.9m, Weight = 0.619m, Color = "#F7931A" },
            new AllocationDto { Name = "Ethereum", Category = "Cryptocurrency", Value = 2000m, Percentage = 19.05m, Weight = 0.1905m, Color = "#627EEA" },
            new AllocationDto { Name = "Cash", Category = "Cash", Value = 2000m, Percentage = 19.05m, Weight = 0.1905m, Color = "#28A745" }
        });

    public Task<List<AllocationDto>> GetSectorAllocationAsync(Guid userId, Guid portfolioId)
        => Task.FromResult(new List<AllocationDto>
        {
            new AllocationDto { Name = "Technology", Category = "Sector", Value = 6500m, Percentage = 61.9m, Weight = 0.619m, Color = "#007BFF" },
            new AllocationDto { Name = "Finance", Category = "Sector", Value = 2000m, Percentage = 19.05m, Weight = 0.1905m, Color = "#28A745" },
            new AllocationDto { Name = "Cash", Category = "Sector", Value = 2000m, Percentage = 19.05m, Weight = 0.1905m, Color = "#FFC107" }
        });

    public Task<PortfolioComparisonDto> ComparePortfoliosAsync(Guid userId, List<Guid> portfolioIds, DateTime? fromDate = null, DateTime? toDate = null)
        => Task.FromResult(new PortfolioComparisonDto
        {
            FromDate = fromDate ?? DateTime.UtcNow.AddMonths(-1),
            ToDate = toDate ?? DateTime.UtcNow,
            BenchmarkSymbol = "SPY",
            Portfolios = portfolioIds.Select((id, index) => new PortfolioAnalysisDto
            {
                PortfolioId = id,
                Name = $"Portfolio {index + 1}",
                TotalReturn = 500m + (index * 100m),
                Volatility = 15.2m + (index * 2m),
                SharpeRatio = 0.68m + (index * 0.1m),
                MaxDrawdown = 125m + (index * 25m),
                DailyReturns = Enumerable.Range(1, 30).Select(i => new DailyReturnDto
                {
                    Date = DateTime.UtcNow.AddDays(-i),
                    Return = (decimal)(new Random().NextDouble() * 4 - 2),
                    CumulativeReturn = (decimal)(i * 0.5 + index)
                }).ToList()
            }).ToList()
        });

    public Task<PortfolioOptimizationDto> GetPortfolioOptimizationAsync(Guid userId, Guid portfolioId, string optimizationType = "MaxSharpe")
        => Task.FromResult(new PortfolioOptimizationDto
        {
            PortfolioId = portfolioId,
            ExpectedReturn = 14.5m,
            ExpectedRisk = 12.8m,
            SharpeRatio = 1.13m,
            OptimizationType = optimizationType,
            RecommendedAllocations = new List<OptimalAllocationDto>
            {
                new OptimalAllocationDto
                {
                    Symbol = "BTCUSDT",
                    SymbolName = "Bitcoin",
                    CurrentWeight = 0.619m,
                    RecommendedWeight = 0.55m,
                    WeightChange = -0.069m,
                    ExpectedReturn = 15.2m,
                    Risk = 18.5m
                },
                new OptimalAllocationDto
                {
                    Symbol = "ETHUSDT",
                    SymbolName = "Ethereum",
                    CurrentWeight = 0.19m,
                    RecommendedWeight = 0.25m,
                    WeightChange = 0.06m,
                    ExpectedReturn = 12.8m,
                    Risk = 16.2m
                },
                new OptimalAllocationDto
                {
                    Symbol = "CASH",
                    SymbolName = "Cash",
                    CurrentWeight = 0.191m,
                    RecommendedWeight = 0.20m,
                    WeightChange = 0.009m,
                    ExpectedReturn = 2.5m,
                    Risk = 0.5m
                }
            }
        });
    public Task<PortfolioSummaryDto?> UpdatePortfolioAsync(Guid userId, Guid portfolioId, UpdatePortfolioDto updateDto) 
        => Task.FromResult<PortfolioSummaryDto?>(null);
    
    public Task<bool> DeletePortfolioAsync(Guid userId, Guid portfolioId) 
        => Task.FromResult(false);
    
    public Task<bool> SetDefaultPortfolioAsync(Guid userId, Guid portfolioId) 
        => Task.FromResult(false);
    
    public Task<List<PortfolioPositionDto>> GetPositionsAsync(Guid userId, Guid? portfolioId = null) 
        => Task.FromResult(new List<PortfolioPositionDto>());
    
    public Task<PortfolioPositionDto?> GetPositionAsync(Guid userId, Guid portfolioId, Guid symbolId) 
        => Task.FromResult<PortfolioPositionDto?>(null);
    
    public Task<TransactionDto> CreateTransactionAsync(Guid userId, CreateTransactionDto createDto) 
        => Task.FromResult(new TransactionDto 
        { 
            Id = Guid.NewGuid(),
            Symbol = "BTCUSDT",
            SymbolName = "Bitcoin",
            TransactionType = createDto.TransactionType,
            Side = createDto.Side,
            Quantity = createDto.Quantity,
            Price = createDto.Price,
            TotalAmount = createDto.Quantity * createDto.Price,
            Fee = createDto.Fee,
            Currency = createDto.Currency,
            Status = "Completed",
            ExecutedAt = createDto.ExecutedAt ?? DateTime.UtcNow
        });
    
    public Task<TransactionHistoryResponseDto> GetTransactionHistoryAsync(Guid userId, TransactionHistoryRequestDto request) 
        => Task.FromResult(new TransactionHistoryResponseDto 
        { 
            Transactions = new List<TransactionDto>
            {
                new TransactionDto
                {
                    Id = Guid.NewGuid(),
                    Symbol = "BTCUSDT",
                    SymbolName = "Bitcoin",
                    TransactionType = "Trade",
                    Side = "BUY",
                    Quantity = 0.1m,
                    Price = 60000m,
                    TotalAmount = 6000m,
                    Fee = 6.0m,
                    Currency = "USD",
                    Status = "Completed",
                    ExecutedAt = DateTime.UtcNow.AddDays(-1)
                }
            },
            TotalCount = 1,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalPages = 1
        });
    
    public Task<List<TransactionDto>> GetRecentTransactionsAsync(Guid userId, int count = 10) 
        => Task.FromResult(new List<TransactionDto>
        {
            new TransactionDto
            {
                Id = Guid.NewGuid(),
                Symbol = "ETHUSDT",
                SymbolName = "Ethereum",
                TransactionType = "Trade",
                Side = "SELL",
                Quantity = 2.0m,
                Price = 4000m,
                TotalAmount = 8000m,
                Fee = 8.0m,
                Currency = "USD",
                Status = "Completed",
                ExecutedAt = DateTime.UtcNow.AddHours(-2)
            },
            new TransactionDto
            {
                Id = Guid.NewGuid(),
                Symbol = "BTCUSDT",
                SymbolName = "Bitcoin",
                TransactionType = "Trade",
                Side = "BUY",
                Quantity = 0.1m,
                Price = 65000m,
                TotalAmount = 6500m,
                Fee = 6.5m,
                Currency = "USD",
                Status = "Completed",
                ExecutedAt = DateTime.UtcNow.AddHours(-1)
            }
        });
    
    public Task<PortfolioPerformanceDto> GetPerformanceAsync(Guid userId, Guid? portfolioId = null) 
        => Task.FromResult(new PortfolioPerformanceDto 
        { 
            TotalReturn = 500m,
            TotalReturnPercent = 5.0m,
            DailyReturn = 100m,
            DailyReturnPercent = 1.0m,
            CalculatedAt = DateTime.UtcNow
        });
    
    public Task<List<PortfolioAllocationDto>> GetAssetAllocationAsync(Guid userId, Guid? portfolioId = null) 
        => Task.FromResult(new List<PortfolioAllocationDto>());
    
    public Task UpdatePortfolioValuesAsync(Guid portfolioId) 
        => Task.CompletedTask;
    
    public Task UpdateAllPortfolioValuesAsync() 
        => Task.CompletedTask;
    
    public Task<decimal> CalculatePortfolioValueAsync(Guid portfolioId) 
        => Task.FromResult(10000m);
    
    public Task<decimal> GetTotalPortfolioValueAsync(Guid userId) 
        => Task.FromResult(10000m);
    
    public Task<decimal> GetDailyPnLAsync(Guid userId, Guid? portfolioId = null) 
        => Task.FromResult(100m);
    
    public Task<Dictionary<string, decimal>> GetPortfolioMetricsAsync(Guid userId, Guid? portfolioId = null) 
        => Task.FromResult(new Dictionary<string, decimal> { { "totalValue", 10000m } });

    // Export & Reporting Methods
    public Task<ExportResponseDto> ExportPortfolioAsync(Guid userId, ExportRequestDto request)
        => Task.FromResult(new ExportResponseDto
        {
            FileName = $"portfolio_export_{DateTime.UtcNow:yyyyMMdd_HHmmss}.{request.ExportType.ToLower()}",
            ContentType = request.ExportType.ToUpper() switch
            {
                "CSV" => "text/csv",
                "PDF" => "application/pdf",
                "EXCEL" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                _ => "application/octet-stream"
            },
            FileContent = GenerateMockFileContent(request),
            FileSizeBytes = 15420,
            GeneratedAt = DateTime.UtcNow,
            DownloadUrl = $"/api/portfolio/export/download/{Guid.NewGuid()}"
        });

    public Task<PortfolioReportDto> GeneratePortfolioReportAsync(Guid userId, Guid portfolioId, DateTime? fromDate = null, DateTime? toDate = null)
        => Task.FromResult(new PortfolioReportDto
        {
            PortfolioId = portfolioId,
            PortfolioName = "Test Portfolio",
            BaseCurrency = "USD",
            ReportDate = DateTime.UtcNow,
            FromDate = fromDate ?? DateTime.UtcNow.AddMonths(-1),
            ToDate = toDate ?? DateTime.UtcNow,
            Summary = new PortfolioSummarySection
            {
                InitialCapital = 10000m,
                CurrentValue = 10500m,
                CashBalance = 2000m,
                TotalPnL = 500m,
                TotalPnLPercent = 5.0m,
                DailyPnL = 25m,
                WeeklyPnL = 175m,
                MonthlyPnL = 300m,
                YearToDatePnL = 500m,
                TotalPositions = 3,
                TotalTransactions = 15,
                LastUpdated = DateTime.UtcNow
            },
            Performance = new PerformanceReportSection
            {
                TotalReturn = 500m,
                AnnualizedReturn = 12.5m,
                BestDay = 3.5m,
                WorstDay = -2.1m,
                WinRate = 60.0m,
                ProfitFactor = 1.5m,
                WinningDays = 18,
                LosingDays = 12,
                MonthlyBreakdown = GenerateMonthlyPerformance()
            },
            Risk = new RiskReportSection
            {
                Volatility = 15.2m,
                SharpeRatio = 0.68m,
                SortinoRatio = 0.95m,
                MaxDrawdown = 125m,
                MaxDrawdownPercent = 1.25m,
                CurrentDrawdown = 0m,
                ValueAtRisk = 200m,
                Beta = 0.85m,
                Alpha = 2.5m
            },
            Holdings = new HoldingsReportSection
            {
                Positions = GeneratePositionReports(),
                AssetAllocation = new List<AllocationDto>
                {
                    new AllocationDto { Name = "Bitcoin", Category = "Cryptocurrency", Value = 6500m, Percentage = 61.9m, Weight = 0.619m, Color = "#F7931A" },
                    new AllocationDto { Name = "Cash", Category = "Cash", Value = 2000m, Percentage = 19.05m, Weight = 0.1905m, Color = "#28A745" },
                    new AllocationDto { Name = "Others", Category = "Mixed", Value = 2000m, Percentage = 19.05m, Weight = 0.1905m, Color = "#6C757D" }
                },
                TotalMarketValue = 10500m,
                TotalHoldings = 3
            },
            Transactions = new TransactionsReportSection
            {
                RecentTransactions = GenerateRecentTransactions(),
                Summary = new TransactionSummaryDto
                {
                    TotalBuyTransactions = 8,
                    TotalSellTransactions = 7,
                    TotalBuyVolume = 45000m,
                    TotalSellVolume = 42000m,
                    TotalFees = 150m,
                    MostTradedSymbol = "BTCUSDT",
                    UniqueTradedSymbols = 5
                },
                TotalTransactionsInPeriod = 15,
                TotalVolumeTraded = 87000m,
                TotalFeesPaid = 150m
            },
            Charts = new ReportChartsSection
            {
                PortfolioValueChart = GenerateChartData("Portfolio Value"),
                PerformanceChart = GenerateChartData("Performance"),
                DrawdownChart = GenerateChartData("Drawdown"),
                AllocationChart = new List<PieChartData>
                {
                    new PieChartData { Label = "Bitcoin", Value = 6500m, Percentage = 61.9m, Color = "#F7931A" },
                    new PieChartData { Label = "Cash", Value = 2000m, Percentage = 19.05m, Color = "#28A745" },
                    new PieChartData { Label = "Others", Value = 2000m, Percentage = 19.05m, Color = "#6C757D" }
                }
            }
        });

    public Task<byte[]> ExportTransactionHistoryAsync(Guid userId, Guid portfolioId, string format = "CSV", DateTime? fromDate = null, DateTime? toDate = null)
        => Task.FromResult(GenerateMockCsvContent("transactions"));

    public Task<byte[]> ExportPerformanceReportAsync(Guid userId, Guid portfolioId, string format = "PDF", DateTime? fromDate = null, DateTime? toDate = null)
        => Task.FromResult(GenerateMockPdfContent());

    public Task<CsvExportDto> GeneratePortfolioCsvAsync(Guid userId, Guid portfolioId, DateTime? fromDate = null, DateTime? toDate = null)
        => Task.FromResult(new CsvExportDto
        {
            FileName = $"portfolio_data_{DateTime.UtcNow:yyyyMMdd}.csv",
            Headers = new List<string> { "Date", "Symbol", "Quantity", "Price", "Value", "PnL", "PnL%" },
            Data = new List<Dictionary<string, object>>
            {
                new Dictionary<string, object> {
                    { "Date", DateTime.UtcNow.ToString("yyyy-MM-dd") },
                    { "Symbol", "BTCUSDT" },
                    { "Quantity", 0.1m },
                    { "Price", 65000m },
                    { "Value", 6500m },
                    { "PnL", 500m },
                    { "PnL%", 8.33m }
                },
                new Dictionary<string, object> {
                    { "Date", DateTime.UtcNow.ToString("yyyy-MM-dd") },
                    { "Symbol", "CASH" },
                    { "Quantity", 2000m },
                    { "Price", 1m },
                    { "Value", 2000m },
                    { "PnL", 0m },
                    { "PnL%", 0m }
                }
            }
        });

    public Task<PdfExportDto> GeneratePortfolioPdfAsync(Guid userId, Guid portfolioId, DateTime? fromDate = null, DateTime? toDate = null)
        => Task.FromResult(new PdfExportDto
        {
            Title = "Portfolio Performance Report",
            Subtitle = $"Generated on {DateTime.UtcNow:MMMM dd, yyyy}",
            ReportData = new PortfolioReportDto
            {
                PortfolioId = portfolioId,
                PortfolioName = "Test Portfolio",
                BaseCurrency = "USD",
                ReportDate = DateTime.UtcNow
            },
            Metadata = new Dictionary<string, object>
            {
                { "Author", "MyTrader Platform" },
                { "Subject", "Portfolio Analysis" },
                { "Keywords", "Portfolio, Trading, Performance, Analytics" },
                { "Creator", "MyTrader Export Engine" }
            }
        });

    // Helper Methods
    private byte[] GenerateMockFileContent(ExportRequestDto request)
    {
        var content = request.ExportType.ToUpper() switch
        {
            "CSV" => "Date,Symbol,Quantity,Price,Value\n2025-09-21,BTCUSDT,0.1,65000,6500\n2025-09-21,CASH,2000,1,2000",
            "PDF" => "Mock PDF Content - Portfolio Report Generated",
            "EXCEL" => "Mock Excel Content - Portfolio Data Export",
            _ => "Mock File Content"
        };
        return System.Text.Encoding.UTF8.GetBytes(content);
    }

    private byte[] GenerateMockCsvContent(string type)
    {
        var content = type switch
        {
            "transactions" => "Date,Type,Symbol,Quantity,Price,Amount,Fee\n2025-09-21,BUY,BTCUSDT,0.1,65000,6500,6.5\n2025-09-20,SELL,ETHUSDT,2.0,4000,8000,8.0",
            _ => "Date,Value\n2025-09-21,10500\n2025-09-20,10400"
        };
        return System.Text.Encoding.UTF8.GetBytes(content);
    }

    private byte[] GenerateMockPdfContent()
    {
        return System.Text.Encoding.UTF8.GetBytes("Mock PDF Report Content - Portfolio Performance Analysis");
    }

    private List<MonthlyPerformanceDto> GenerateMonthlyPerformance()
    {
        return Enumerable.Range(1, 6).Select(i => new MonthlyPerformanceDto
        {
            Year = 2025,
            Month = 9 - i,
            MonthName = new DateTime(2025, 9 - i, 1).ToString("MMMM"),
            Return = (decimal)(new Random().NextDouble() * 1000 - 500),
            ReturnPercent = (decimal)(new Random().NextDouble() * 10 - 5),
            StartValue = 10000m + (i * 100m),
            EndValue = 10000m + ((i + 1) * 100m),
            TradingDays = 22
        }).ToList();
    }

    private List<PositionReportDto> GeneratePositionReports()
    {
        return new List<PositionReportDto>
        {
            new PositionReportDto
            {
                Symbol = "BTCUSDT",
                SymbolName = "Bitcoin",
                Quantity = 0.1m,
                AveragePrice = 60000m,
                CurrentPrice = 65000m,
                MarketValue = 6500m,
                UnrealizedPnL = 500m,
                UnrealizedPnLPercent = 8.33m,
                Weight = 61.9m,
                FirstPurchaseDate = DateTime.UtcNow.AddMonths(-2),
                LastTradeDate = DateTime.UtcNow.AddDays(-1)
            },
            new PositionReportDto
            {
                Symbol = "CASH",
                SymbolName = "US Dollar",
                Quantity = 2000m,
                AveragePrice = 1m,
                CurrentPrice = 1m,
                MarketValue = 2000m,
                UnrealizedPnL = 0m,
                UnrealizedPnLPercent = 0m,
                Weight = 19.05m,
                FirstPurchaseDate = DateTime.UtcNow.AddMonths(-3),
                LastTradeDate = DateTime.UtcNow.AddDays(-5)
            }
        };
    }

    private List<TransactionDto> GenerateRecentTransactions()
    {
        return new List<TransactionDto>
        {
            new TransactionDto
            {
                Id = Guid.NewGuid(),
                Symbol = "BTCUSDT",
                SymbolName = "Bitcoin",
                TransactionType = "BUY",
                Side = "LONG",
                Quantity = 0.05m,
                Price = 65000m,
                TotalAmount = 3250m,
                Fee = 3.25m,
                Currency = "USD",
                Status = "Completed",
                ExecutedAt = DateTime.UtcNow.AddDays(-1)
            },
            new TransactionDto
            {
                Id = Guid.NewGuid(),
                Symbol = "ETHUSDT",
                SymbolName = "Ethereum",
                TransactionType = "SELL",
                Side = "LONG",
                Quantity = 1.0m,
                Price = 4000m,
                TotalAmount = 4000m,
                Fee = 4.0m,
                Currency = "USD",
                Status = "Completed",
                ExecutedAt = DateTime.UtcNow.AddDays(-2)
            }
        };
    }

    private List<ChartDataPoint> GenerateChartData(string type)
    {
        return Enumerable.Range(1, 30).Select(i => new ChartDataPoint
        {
            Date = DateTime.UtcNow.AddDays(-i),
            Value = type switch
            {
                "Portfolio Value" => 10000m + (i * 10m) + (decimal)(new Random().NextDouble() * 200 - 100),
                "Performance" => (decimal)(new Random().NextDouble() * 4 - 2),
                "Drawdown" => Math.Max(0, (decimal)(new Random().NextDouble() * 50)),
                _ => (decimal)(new Random().NextDouble() * 100)
            },
            Label = type
        }).ToList();
    }
}