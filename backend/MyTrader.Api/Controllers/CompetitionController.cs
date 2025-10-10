using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace MyTrader.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class CompetitionController : ControllerBase
{
    private readonly ILogger<CompetitionController> _logger;

    public CompetitionController(ILogger<CompetitionController> logger)
    {
        _logger = logger;
    }

    [HttpPost("join")]
    [AllowAnonymous] // Test için
    public IActionResult JoinCompetition()
    {
        try
        {
            _logger.LogInformation("Competition join request received");

            var competitionId = Guid.NewGuid();
            var response = new
            {
                success = true,
                message = "Competition joined successfully",
                competitionId = competitionId,
                joinedAt = DateTime.UtcNow,
                competition = new
                {
                    id = competitionId,
                    name = "MyTrader Demo Competition",
                    status = "active",
                    startDate = DateTime.UtcNow.Date,
                    endDate = DateTime.UtcNow.Date.AddDays(30),
                    // CRITICAL: Always provide prizes array to prevent frontend crashes
                    prizes = new[]
                    {
                        new { rank = 1, amount = "$5,000", description = "First Place Winner" },
                        new { rank = 2, amount = "$3,000", description = "Second Place Winner" },
                        new { rank = 3, amount = "$2,000", description = "Third Place Winner" },
                        new { rank = 4, amount = "$500", description = "Top 10 Participants" },
                        new { rank = 5, amount = "$250", description = "Top 20 Participants" }
                    }
                }
            };

            _logger.LogInformation("Competition joined successfully with ID: {CompetitionId}", competitionId);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error joining competition");
            return StatusCode(500, new {
                success = false,
                message = "Internal server error occurred while joining competition",
                // Provide empty competition data structure even in error case
                competition = new
                {
                    id = (Guid?)null,
                    name = "",
                    status = "error",
                    startDate = (DateTime?)null,
                    endDate = (DateTime?)null,
                    prizes = new object[0] // Empty array, not null
                }
            });
        }
    }

    [HttpGet("status")]
    [AllowAnonymous]
    public IActionResult GetCompetitionStatus()
    {
        try
        {
            var response = new
            {
                success = true,
                competition = new
                {
                    id = Guid.NewGuid(),
                    name = "MyTrader Demo Competition",
                    startDate = DateTime.UtcNow.Date,
                    endDate = DateTime.UtcNow.Date.AddDays(30),
                    participants = 125,
                    prizePool = "$10,000",
                    status = "active",
                    // CRITICAL: Always provide prizes array for frontend compatibility
                    prizes = new[]
                    {
                        new { rank = 1, amount = "$5,000", description = "First Place Winner", percentage = 50.0 },
                        new { rank = 2, amount = "$3,000", description = "Second Place Winner", percentage = 30.0 },
                        new { rank = 3, amount = "$2,000", description = "Third Place Winner", percentage = 20.0 }
                    }
                }
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting competition status");
            return StatusCode(500, new {
                success = false,
                message = "Internal server error occurred while getting competition status",
                // Provide safe data structure even in error case
                competition = new
                {
                    id = (Guid?)null,
                    name = "",
                    startDate = (DateTime?)null,
                    endDate = (DateTime?)null,
                    participants = 0,
                    prizePool = "$0",
                    status = "error",
                    prizes = new object[0] // Empty array, not null
                }
            });
        }
    }

    [HttpGet("competition-leaderboard")]
    [AllowAnonymous]
    public IActionResult GetCompetitionLeaderboard()
    {
        try
        {
            var response = new
            {
                success = true,
                // CRITICAL: Always provide as array for frontend compatibility
                leaderboard = new[]
                {
                    new { rank = 1, username = "TraderPro", returns = "15.8%", value = "$115,800", userId = Guid.NewGuid() },
                    new { rank = 2, username = "MarketMaster", returns = "12.3%", value = "$112,300", userId = Guid.NewGuid() },
                    new { rank = 3, username = "StockWizard", returns = "9.7%", value = "$109,700", userId = Guid.NewGuid() },
                    new { rank = 4, username = "CryptoKing", returns = "8.2%", value = "$108,200", userId = Guid.NewGuid() },
                    new { rank = 5, username = "InvestorAce", returns = "6.9%", value = "$106,900", userId = Guid.NewGuid() }
                },
                userRank = new { rank = 42, username = "YourUsername", returns = "2.1%", value = "$102,100", userId = (Guid?)null },
                totalParticipants = 125,
                timestamp = DateTime.UtcNow
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting competition leaderboard");
            return StatusCode(500, new {
                success = false,
                message = "Internal server error occurred while getting leaderboard",
                // CRITICAL: Provide safe array structure even in error case
                leaderboard = new object[0], // Empty array, not null
                userRank = new { rank = 0, username = "", returns = "0%", value = "$0", userId = (Guid?)null },
                totalParticipants = 0,
                timestamp = DateTime.UtcNow
            });
        }
    }
}