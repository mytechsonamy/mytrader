using MyTrader.Core.DTOs.Portfolio;
using MyTrader.Core.Models;

namespace MyTrader.Core.Interfaces;

public interface IPortfolioService
{
    // Portfolio Management
    Task<PortfolioSummaryDto?> GetPortfolioAsync(Guid userId, Guid? portfolioId = null);
    Task<List<PortfolioSummaryDto>> GetUserPortfoliosAsync(Guid userId);
    Task<PortfolioSummaryDto> CreatePortfolioAsync(Guid userId, CreatePortfolioDto createDto);
    Task<PortfolioSummaryDto?> UpdatePortfolioAsync(Guid userId, Guid portfolioId, UpdatePortfolioDto updateDto);
    Task<bool> DeletePortfolioAsync(Guid userId, Guid portfolioId);
    Task<bool> SetDefaultPortfolioAsync(Guid userId, Guid portfolioId);

    // Position Management
    Task<List<PortfolioPositionDto>> GetPositionsAsync(Guid userId, Guid? portfolioId = null);
    Task<PortfolioPositionDto?> GetPositionAsync(Guid userId, Guid portfolioId, Guid symbolId);
    
    // Transaction Management
    Task<TransactionDto> CreateTransactionAsync(Guid userId, CreateTransactionDto createDto);
    Task<TransactionHistoryResponseDto> GetTransactionHistoryAsync(Guid userId, TransactionHistoryRequestDto request);
    Task<List<TransactionDto>> GetRecentTransactionsAsync(Guid userId, int count = 10);

    // Portfolio Analytics
    Task<PortfolioPerformanceDto> GetPerformanceAsync(Guid userId, Guid? portfolioId = null);
    Task<List<PortfolioAllocationDto>> GetAssetAllocationAsync(Guid userId, Guid? portfolioId = null);
    Task<PortfolioAnalyticsDto> GetPortfolioAnalyticsAsync(Guid userId, AnalyticsRequestDto request);
    Task<PerformanceMetricsDto> GetPerformanceMetricsAsync(Guid userId, Guid portfolioId, DateTime? fromDate = null, DateTime? toDate = null);
    Task<RiskMetricsDto> GetRiskMetricsAsync(Guid userId, Guid portfolioId, DateTime? fromDate = null, DateTime? toDate = null);
    Task<List<AllocationDto>> GetAssetAllocationDetailAsync(Guid userId, Guid portfolioId);
    Task<List<AllocationDto>> GetSectorAllocationAsync(Guid userId, Guid portfolioId);
    Task<PortfolioComparisonDto> ComparePortfoliosAsync(Guid userId, List<Guid> portfolioIds, DateTime? fromDate = null, DateTime? toDate = null);
    Task<PortfolioOptimizationDto> GetPortfolioOptimizationAsync(Guid userId, Guid portfolioId, string optimizationType = "MaxSharpe");
    
    // Portfolio Export & Reporting
    Task<ExportResponseDto> ExportPortfolioAsync(Guid userId, ExportRequestDto request);
    Task<PortfolioReportDto> GeneratePortfolioReportAsync(Guid userId, Guid portfolioId, DateTime? fromDate = null, DateTime? toDate = null);
    Task<byte[]> ExportTransactionHistoryAsync(Guid userId, Guid portfolioId, string format = "CSV", DateTime? fromDate = null, DateTime? toDate = null);
    Task<byte[]> ExportPerformanceReportAsync(Guid userId, Guid portfolioId, string format = "PDF", DateTime? fromDate = null, DateTime? toDate = null);
    Task<CsvExportDto> GeneratePortfolioCsvAsync(Guid userId, Guid portfolioId, DateTime? fromDate = null, DateTime? toDate = null);
    Task<PdfExportDto> GeneratePortfolioPdfAsync(Guid userId, Guid portfolioId, DateTime? fromDate = null, DateTime? toDate = null);
    
    // Real-time Updates
    Task UpdatePortfolioValuesAsync(Guid portfolioId);
    Task UpdateAllPortfolioValuesAsync();
    Task<decimal> CalculatePortfolioValueAsync(Guid portfolioId);
    
    // Portfolio Health & Statistics
    Task<decimal> GetTotalPortfolioValueAsync(Guid userId);
    Task<decimal> GetDailyPnLAsync(Guid userId, Guid? portfolioId = null);
    Task<Dictionary<string, decimal>> GetPortfolioMetricsAsync(Guid userId, Guid? portfolioId = null);
}