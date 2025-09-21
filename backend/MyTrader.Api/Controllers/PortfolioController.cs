using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using MyTrader.Core.DTOs.Portfolio;
using MyTrader.Core.Interfaces;
using MyTrader.API.Hubs;
using System.Security.Claims;

namespace MyTrader.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous] // Temporarily allow anonymous access for testing
public class PortfolioController : ControllerBase
{
    private readonly IPortfolioService _portfolioService;
    private readonly IHubContext<PortfolioHub> _portfolioHubContext;
    private readonly ILogger<PortfolioController> _logger;

    public PortfolioController(
        IPortfolioService portfolioService, 
        IHubContext<PortfolioHub> portfolioHubContext,
        ILogger<PortfolioController> logger)
    {
        _portfolioService = portfolioService;
        _portfolioHubContext = portfolioHubContext;
        _logger = logger;
    }

    private Guid GetUserId()
    {
        // For testing - get first user from database
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim != null)
        {
            return Guid.Parse(userIdClaim);
        }
        
        // Fallback for testing - use first user ID
        // TODO: Remove this when authentication is working
        return Guid.Parse("d0d2c2d4-59d3-472d-b395-a6adf3b20cab"); // hardcoded test user ID
    }

    /// <summary>
    /// Test endpoint to verify controller is working
    /// </summary>
    [HttpGet("test")]
    public ActionResult Test()
    {
        return Ok(new { message = "Portfolio Controller is working!", timestamp = DateTime.UtcNow });
    }

    /// <summary>
    /// Get user's default portfolio or specific portfolio by ID
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PortfolioSummaryDto>> GetPortfolio([FromQuery] Guid? portfolioId = null)
    {
        try
        {
            var userId = GetUserId();
            var portfolio = await _portfolioService.GetPortfolioAsync(userId, portfolioId);

            if (portfolio == null)
                return NotFound("Portfolio not found");

            return Ok(portfolio);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting portfolio");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get all portfolios for the authenticated user
    /// </summary>
    [HttpGet("all")]
    public async Task<ActionResult<List<PortfolioSummaryDto>>> GetUserPortfolios()
    {
        try
        {
            var userId = GetUserId();
            var portfolios = await _portfolioService.GetUserPortfoliosAsync(userId);

            return Ok(portfolios);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user portfolios");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Create a new portfolio
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<PortfolioSummaryDto>> CreatePortfolio([FromBody] CreatePortfolioDto createDto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = GetUserId();
            var portfolio = await _portfolioService.CreatePortfolioAsync(userId, createDto);

            return CreatedAtAction(
                nameof(GetPortfolio),
                new { portfolioId = portfolio.Id },
                portfolio);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating portfolio");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Update portfolio details
    /// </summary>
    [HttpPut("{portfolioId}")]
    public async Task<ActionResult<PortfolioSummaryDto>> UpdatePortfolio(
        Guid portfolioId,
        [FromBody] UpdatePortfolioDto updateDto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = GetUserId();
            var portfolio = await _portfolioService.UpdatePortfolioAsync(userId, portfolioId, updateDto);

            if (portfolio == null)
                return NotFound("Portfolio not found");

            return Ok(portfolio);
        }
        catch (NotImplementedException)
        {
            return StatusCode(501, "Feature not yet implemented");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating portfolio {PortfolioId}", portfolioId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Delete a portfolio
    /// </summary>
    [HttpDelete("{portfolioId}")]
    public async Task<ActionResult> DeletePortfolio(Guid portfolioId)
    {
        try
        {
            var userId = GetUserId();
            var success = await _portfolioService.DeletePortfolioAsync(userId, portfolioId);

            if (!success)
                return NotFound("Portfolio not found");

            return NoContent();
        }
        catch (NotImplementedException)
        {
            return StatusCode(501, "Feature not yet implemented");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting portfolio {PortfolioId}", portfolioId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Set a portfolio as the default portfolio
    /// </summary>
    [HttpPost("{portfolioId}/set-default")]
    public async Task<ActionResult> SetDefaultPortfolio(Guid portfolioId)
    {
        try
        {
            var userId = GetUserId();
            var success = await _portfolioService.SetDefaultPortfolioAsync(userId, portfolioId);

            if (!success)
                return NotFound("Portfolio not found");

            return Ok(new { message = "Default portfolio updated successfully" });
        }
        catch (NotImplementedException)
        {
            return StatusCode(501, "Feature not yet implemented");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting default portfolio {PortfolioId}", portfolioId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get portfolio positions
    /// </summary>
    [HttpGet("positions")]
    public async Task<ActionResult<List<PortfolioPositionDto>>> GetPositions([FromQuery] Guid? portfolioId = null)
    {
        try
        {
            var userId = GetUserId();
            var positions = await _portfolioService.GetPositionsAsync(userId, portfolioId);

            return Ok(positions);
        }
        catch (NotImplementedException)
        {
            return StatusCode(501, "Feature not yet implemented");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting positions");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get specific position in portfolio
    /// </summary>
    [HttpGet("{portfolioId}/positions/{symbolId}")]
    public async Task<ActionResult<PortfolioPositionDto>> GetPosition(Guid portfolioId, Guid symbolId)
    {
        try
        {
            var userId = GetUserId();
            var position = await _portfolioService.GetPositionAsync(userId, portfolioId, symbolId);

            if (position == null)
                return NotFound("Position not found");

            return Ok(position);
        }
        catch (NotImplementedException)
        {
            return StatusCode(501, "Feature not yet implemented");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting position for portfolio {PortfolioId}, symbol {SymbolId}", portfolioId, symbolId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Create a new transaction (buy/sell)
    /// </summary>
        [HttpPost("transactions")]
    [AllowAnonymous] // For testing purposes
    public async Task<ActionResult<TransactionDto>> CreateTransaction(
        [FromQuery] Guid userId,
        [FromBody] CreateTransactionDto createTransactionDto)
    {
        try
        {
            var transaction = await _portfolioService.CreateTransactionAsync(userId, createTransactionDto);
            
            // Send real-time notification
            await _portfolioHubContext.NotifyNewTransaction(
                userId.ToString(), 
                transaction);

            _logger.LogInformation("Transaction created successfully for user {UserId}: {Symbol} {Side} {Quantity}",
                userId, createTransactionDto.TransactionType, createTransactionDto.Side, createTransactionDto.Quantity);

            return Ok(transaction);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating transaction for user {UserId}", userId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get transaction history with filtering and pagination
    /// </summary>
    [HttpGet("transactions")]
    public async Task<ActionResult<TransactionHistoryResponseDto>> GetTransactionHistory([FromQuery] TransactionHistoryRequestDto request)
    {
        try
        {
            var userId = GetUserId();
            var history = await _portfolioService.GetTransactionHistoryAsync(userId, request);

            return Ok(history);
        }
        catch (NotImplementedException)
        {
            return StatusCode(501, "Feature not yet implemented");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting transaction history");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get recent transactions
    /// </summary>
    [HttpGet("transactions/recent")]
    public async Task<ActionResult<List<TransactionDto>>> GetRecentTransactions([FromQuery] int count = 10)
    {
        try
        {
            var userId = GetUserId();
            var transactions = await _portfolioService.GetRecentTransactionsAsync(userId, count);

            return Ok(transactions);
        }
        catch (NotImplementedException)
        {
            return StatusCode(501, "Feature not yet implemented");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recent transactions");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get portfolio performance metrics
    /// </summary>
    [HttpGet("performance")]
    public async Task<ActionResult<PortfolioPerformanceDto>> GetPerformance([FromQuery] Guid? portfolioId = null)
    {
        try
        {
            var userId = GetUserId();
            var performance = await _portfolioService.GetPerformanceAsync(userId, portfolioId);

            return Ok(performance);
        }
        catch (NotImplementedException)
        {
            return StatusCode(501, "Feature not yet implemented");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting portfolio performance");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get asset allocation breakdown
    /// </summary>
    [HttpGet("allocation")]
    public async Task<ActionResult<List<PortfolioAllocationDto>>> GetAssetAllocation([FromQuery] Guid? portfolioId = null)
    {
        try
        {
            var userId = GetUserId();
            var allocation = await _portfolioService.GetAssetAllocationAsync(userId, portfolioId);

            return Ok(allocation);
        }
        catch (NotImplementedException)
        {
            return StatusCode(501, "Feature not yet implemented");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting asset allocation");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get portfolio metrics summary
    /// </summary>
    [HttpGet("metrics")]
    public async Task<ActionResult<Dictionary<string, decimal>>> GetMetrics([FromQuery] Guid? portfolioId = null)
    {
        try
        {
            var userId = GetUserId();
            var metrics = await _portfolioService.GetPortfolioMetricsAsync(userId, portfolioId);

            return Ok(metrics);
        }
        catch (NotImplementedException)
        {
            return StatusCode(501, "Feature not yet implemented");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting portfolio metrics");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Force refresh portfolio values from current market prices
    /// </summary>
    [HttpPost("{portfolioId}/refresh")]
    public async Task<ActionResult> RefreshPortfolioValues(Guid portfolioId)
    {
        try
        {
            await _portfolioService.UpdatePortfolioValuesAsync(portfolioId);
            return Ok(new { message = "Portfolio values refreshed successfully" });
        }
        catch (NotImplementedException)
        {
            return StatusCode(501, "Feature not yet implemented");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing portfolio values for {PortfolioId}", portfolioId);
            return StatusCode(500, "Internal server error");
        }
    }

    // ANALYTICS ENDPOINTS

    [HttpGet("{portfolioId}/analytics")]
    [AllowAnonymous] // For testing purposes
    public async Task<ActionResult<PortfolioAnalyticsDto>> GetPortfolioAnalytics(
        Guid portfolioId,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] string? benchmarkSymbol = null)
    {
        try
        {
            var request = new AnalyticsRequestDto
            {
                PortfolioId = portfolioId,
                FromDate = fromDate,
                ToDate = toDate,
                BenchmarkSymbol = benchmarkSymbol,
                IncludeRiskMetrics = true,
                IncludeAllocation = true,
                IncludePerformanceHistory = true
            };

            var analytics = await _portfolioService.GetPortfolioAnalyticsAsync(Guid.Empty, request);
            
            _logger.LogInformation("Portfolio analytics retrieved for portfolio {PortfolioId}", portfolioId);
            return Ok(analytics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving portfolio analytics for portfolio {PortfolioId}", portfolioId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("{portfolioId}/performance")]
    [AllowAnonymous] // For testing purposes
    public async Task<ActionResult<PerformanceMetricsDto>> GetPerformanceMetrics(
        Guid portfolioId,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        try
        {
            var performance = await _portfolioService.GetPerformanceMetricsAsync(Guid.Empty, portfolioId, fromDate, toDate);
            
            _logger.LogInformation("Performance metrics retrieved for portfolio {PortfolioId}", portfolioId);
            return Ok(performance);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving performance metrics for portfolio {PortfolioId}", portfolioId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("{portfolioId}/risk")]
    [AllowAnonymous] // For testing purposes
    public async Task<ActionResult<RiskMetricsDto>> GetRiskMetrics(
        Guid portfolioId,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        try
        {
            var risk = await _portfolioService.GetRiskMetricsAsync(Guid.Empty, portfolioId, fromDate, toDate);
            
            _logger.LogInformation("Risk metrics retrieved for portfolio {PortfolioId}", portfolioId);
            return Ok(risk);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving risk metrics for portfolio {PortfolioId}", portfolioId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("{portfolioId}/allocation")]
    [AllowAnonymous] // For testing purposes
    public async Task<ActionResult<List<AllocationDto>>> GetAssetAllocation(Guid portfolioId)
    {
        try
        {
            var allocation = await _portfolioService.GetAssetAllocationDetailAsync(Guid.Empty, portfolioId);
            
            _logger.LogInformation("Asset allocation retrieved for portfolio {PortfolioId}", portfolioId);
            return Ok(allocation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving asset allocation for portfolio {PortfolioId}", portfolioId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("{portfolioId}/sectors")]
    [AllowAnonymous] // For testing purposes
    public async Task<ActionResult<List<AllocationDto>>> GetSectorAllocation(Guid portfolioId)
    {
        try
        {
            var sectors = await _portfolioService.GetSectorAllocationAsync(Guid.Empty, portfolioId);
            
            _logger.LogInformation("Sector allocation retrieved for portfolio {PortfolioId}", portfolioId);
            return Ok(sectors);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving sector allocation for portfolio {PortfolioId}", portfolioId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("{portfolioId}/optimization")]
    [AllowAnonymous] // For testing purposes
    public async Task<ActionResult<PortfolioOptimizationDto>> GetPortfolioOptimization(
        Guid portfolioId,
        [FromQuery] string optimizationType = "MaxSharpe")
    {
        try
        {
            var optimization = await _portfolioService.GetPortfolioOptimizationAsync(
                Guid.Empty, 
                portfolioId, 
                optimizationType);
            
            _logger.LogInformation("Portfolio optimization calculated for portfolio {PortfolioId} using {OptimizationType}", 
                portfolioId, optimizationType);
            return Ok(optimization);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating portfolio optimization for portfolio {PortfolioId}", portfolioId);
            return StatusCode(500, "Internal server error");
        }
    }

    // EXPORT & REPORTING ENDPOINTS

    [HttpPost("export")]
    [AllowAnonymous] // For testing purposes
    public async Task<ActionResult<ExportResponseDto>> ExportPortfolio([FromBody] ExportRequestDto request)
    {
        try
        {
            var export = await _portfolioService.ExportPortfolioAsync(Guid.Empty, request);
            
            _logger.LogInformation("Portfolio export generated: {FileName} ({FileSize} bytes)", 
                export.FileName, export.FileSizeBytes);
            return Ok(export);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting portfolio {PortfolioId}", request.PortfolioId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("{portfolioId}/report")]
    [AllowAnonymous] // For testing purposes
    public async Task<ActionResult<PortfolioReportDto>> GeneratePortfolioReport(
        Guid portfolioId,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        try
        {
            var report = await _portfolioService.GeneratePortfolioReportAsync(
                Guid.Empty, 
                portfolioId, 
                fromDate, 
                toDate);
            
            _logger.LogInformation("Portfolio report generated for portfolio {PortfolioId}", portfolioId);
            return Ok(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating portfolio report for {PortfolioId}", portfolioId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("{portfolioId}/export/transactions")]
    [AllowAnonymous] // For testing purposes
    public async Task<ActionResult> ExportTransactionHistory(
        Guid portfolioId,
        [FromQuery] string format = "CSV",
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        try
        {
            var fileContent = await _portfolioService.ExportTransactionHistoryAsync(
                Guid.Empty, 
                portfolioId, 
                format, 
                fromDate, 
                toDate);

            var fileName = $"transactions_{portfolioId}_{DateTime.UtcNow:yyyyMMdd}.{format.ToLower()}";
            var contentType = format.ToUpper() switch
            {
                "CSV" => "text/csv",
                "PDF" => "application/pdf",
                "EXCEL" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                _ => "application/octet-stream"
            };

            _logger.LogInformation("Transaction history exported for portfolio {PortfolioId} in {Format} format", 
                portfolioId, format);

            return File(fileContent, contentType, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting transaction history for portfolio {PortfolioId}", portfolioId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("{portfolioId}/export/performance")]
    [AllowAnonymous] // For testing purposes
    public async Task<ActionResult> ExportPerformanceReport(
        Guid portfolioId,
        [FromQuery] string format = "PDF",
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        try
        {
            var fileContent = await _portfolioService.ExportPerformanceReportAsync(
                Guid.Empty, 
                portfolioId, 
                format, 
                fromDate, 
                toDate);

            var fileName = $"performance_report_{portfolioId}_{DateTime.UtcNow:yyyyMMdd}.{format.ToLower()}";
            var contentType = format.ToUpper() switch
            {
                "PDF" => "application/pdf",
                "CSV" => "text/csv",
                _ => "application/octet-stream"
            };

            _logger.LogInformation("Performance report exported for portfolio {PortfolioId} in {Format} format", 
                portfolioId, format);

            return File(fileContent, contentType, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting performance report for portfolio {PortfolioId}", portfolioId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("{portfolioId}/export/csv")]
    [AllowAnonymous] // For testing purposes
    public async Task<ActionResult<CsvExportDto>> GeneratePortfolioCsv(
        Guid portfolioId,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        try
        {
            var csvData = await _portfolioService.GeneratePortfolioCsvAsync(
                Guid.Empty, 
                portfolioId, 
                fromDate, 
                toDate);
            
            _logger.LogInformation("CSV export data generated for portfolio {PortfolioId}", portfolioId);
            return Ok(csvData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating CSV data for portfolio {PortfolioId}", portfolioId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("{portfolioId}/export/pdf")]
    [AllowAnonymous] // For testing purposes
    public async Task<ActionResult<PdfExportDto>> GeneratePortfolioPdf(
        Guid portfolioId,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        try
        {
            var pdfData = await _portfolioService.GeneratePortfolioPdfAsync(
                Guid.Empty, 
                portfolioId, 
                fromDate, 
                toDate);
            
            _logger.LogInformation("PDF export data generated for portfolio {PortfolioId}", portfolioId);
            return Ok(pdfData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating PDF data for portfolio {PortfolioId}", portfolioId);
            return StatusCode(500, "Internal server error");
        }
    }
}