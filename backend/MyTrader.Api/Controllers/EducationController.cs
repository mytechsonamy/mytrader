using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyTrader.Services.Education;
using System.Security.Claims;

namespace MyTrader.Api.Controllers;

[ApiController]
[Route("api/v1/education")]
[Tags("Education & Learning")]
public class EducationController : ControllerBase
{
    private readonly IEducationService _educationService;

    public EducationController(IEducationService educationService)
    {
        _educationService = educationService;
    }

    private Guid? GetUserId()
    {
        var claim = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return claim != null ? Guid.Parse(claim) : null;
    }

    [HttpGet("modules")]
    public async Task<ActionResult> GetLearningModules([FromQuery] string? category = null, [FromQuery] string? difficulty = null)
    {
        try
        {
            var modules = await _educationService.GetLearningModulesAsync(category, difficulty);

            return Ok(new
            {
                success = true,
                data = modules.Select(m => new
                {
                    id = m.Id,
                    title = m.Title,
                    description = m.Description,
                    category = m.Category,
                    difficulty = m.Difficulty,
                    estimated_minutes = m.EstimatedMinutes,
                    thumbnail_url = m.ThumbnailUrl,
                    tags = m.Tags,
                    lesson_count = m.Lessons.Count,
                    progress = m.Progress != null ? new
                    {
                        completion_percentage = m.Progress.CompletionPercentage,
                        completed_lessons = m.Progress.CompletedLessons,
                        total_lessons = m.Progress.TotalLessons,
                        time_spent_minutes = m.Progress.TimeSpentMinutes
                    } : null,
                    order_index = m.OrderIndex,
                    created_at = m.CreatedAt
                })
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Failed to get learning modules" });
        }
    }

    [HttpGet("modules/{moduleId}")]
    [Authorize]
    public async Task<ActionResult> GetLearningModule(string moduleId)
    {
        try
        {
            if (!Guid.TryParse(moduleId, out var moduleGuid))
            {
                return BadRequest(new { success = false, message = "Invalid module ID" });
            }

            var userId = GetUserId();
            var module = await _educationService.GetLearningModuleAsync(moduleGuid, userId);

            if (module == null)
            {
                return NotFound(new { success = false, message = "Module not found" });
            }

            return Ok(new
            {
                success = true,
                data = new
                {
                    id = module.Id,
                    title = module.Title,
                    description = module.Description,
                    category = module.Category,
                    difficulty = module.Difficulty,
                    estimated_minutes = module.EstimatedMinutes,
                    thumbnail_url = module.ThumbnailUrl,
                    tags = module.Tags,
                    lessons = module.Lessons.Select(l => new
                    {
                        id = l.Id,
                        title = l.Title,
                        lesson_type = l.LessonType,
                        estimated_minutes = l.EstimatedMinutes,
                        key_points = l.KeyPoints,
                        order_index = l.OrderIndex,
                        is_completed = l.IsCompleted,
                        has_quiz = l.Quiz != null,
                        has_interactive = l.Interactive != null
                    }),
                    progress = module.Progress != null ? new
                    {
                        completion_percentage = module.Progress.CompletionPercentage,
                        completed_lessons = module.Progress.CompletedLessons,
                        total_lessons = module.Progress.TotalLessons,
                        time_spent_minutes = module.Progress.TimeSpentMinutes,
                        started_at = module.Progress.StartedAt,
                        last_accessed_at = module.Progress.LastAccessedAt
                    } : null
                }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Failed to get learning module" });
        }
    }

    [HttpGet("paths")]
    public async Task<ActionResult> GetLearningPaths([FromQuery] string? category = null)
    {
        try
        {
            var paths = await _educationService.GetLearningPathsAsync(category);

            return Ok(new
            {
                success = true,
                data = paths.Select(p => new
                {
                    id = p.Id,
                    name = p.Name,
                    description = p.Description,
                    category = p.Category,
                    difficulty = p.Difficulty,
                    estimated_hours = p.EstimatedHours,
                    module_count = p.ModuleIds.Count,
                    thumbnail_url = p.ThumbnailUrl,
                    prerequisite_paths = p.PrerequisitePaths,
                    is_recommended = p.IsRecommended,
                    created_at = p.CreatedAt
                })
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Failed to get learning paths" });
        }
    }

    [HttpGet("tutorials")]
    public async Task<ActionResult> GetTutorials([FromQuery] string? category = null, [FromQuery] string? targetUrl = null)
    {
        try
        {
            var tutorials = await _educationService.GetTutorialsAsync(category, targetUrl);

            return Ok(new
            {
                success = true,
                data = tutorials.Select(t => new
                {
                    id = t.Id,
                    name = t.Name,
                    description = t.Description,
                    category = t.Category,
                    target_url = t.TargetUrl,
                    target_user_type = t.TargetUserType,
                    priority = t.Priority,
                    step_count = t.Steps.Count,
                    is_active = t.IsActive,
                    steps = t.Steps.Select(s => new
                    {
                        step_number = s.StepNumber,
                        title = s.Title,
                        description = s.Description,
                        target_element = s.TargetElement,
                        position = s.Position,
                        action = s.Action,
                        next_trigger = s.NextTrigger
                    })
                })
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Failed to get tutorials" });
        }
    }

    [HttpGet("profile")]
    [Authorize]
    public async Task<ActionResult> GetLearningProfile()
    {
        try
        {
            var userId = GetUserId();
            if (!userId.HasValue)
            {
                return Unauthorized();
            }

            var profile = await _educationService.GetLearningProfileAsync(userId.Value);

            return Ok(new
            {
                success = true,
                data = new
                {
                    user_id = profile.UserId,
                    learning_style = profile.LearningStyle,
                    interests = profile.Interests,
                    experience = profile.Experience,
                    completed_modules = profile.CompletedModules,
                    completed_paths = profile.CompletedPaths,
                    bookmarked_content = profile.BookmarkedContent,
                    category_progress = profile.CategoryProgress,
                    total_learning_minutes = profile.TotalLearningMinutes,
                    last_learning_activity = profile.LastLearningActivity,
                    preferences = profile.Preferences
                }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Failed to get learning profile" });
        }
    }

    [HttpGet("recommendations/modules")]
    [Authorize]
    public async Task<ActionResult> GetRecommendedModules([FromQuery] int limit = 5)
    {
        try
        {
            var userId = GetUserId();
            if (!userId.HasValue)
            {
                return Unauthorized();
            }

            var recommendations = await _educationService.GetRecommendedModulesAsync(userId.Value, limit);

            return Ok(new
            {
                success = true,
                data = recommendations.Select(m => new
                {
                    id = m.Id,
                    title = m.Title,
                    description = m.Description,
                    category = m.Category,
                    difficulty = m.Difficulty,
                    estimated_minutes = m.EstimatedMinutes,
                    thumbnail_url = m.ThumbnailUrl,
                    tags = m.Tags,
                    lesson_count = m.Lessons.Count,
                    reason = "Based on your interests and experience level"
                })
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Failed to get recommended modules" });
        }
    }

    [HttpGet("dashboard")]
    [Authorize]
    public async Task<ActionResult> GetEducationDashboard()
    {
        try
        {
            var userId = GetUserId();
            if (!userId.HasValue)
            {
                return Unauthorized();
            }

            // Get multiple education data in parallel
            var profileTask = _educationService.GetLearningProfileAsync(userId.Value);
            var recommendedModulesTask = _educationService.GetRecommendedModulesAsync(userId.Value, 3);
            var allModulesTask = _educationService.GetLearningModulesAsync();
            var pathsTask = _educationService.GetLearningPathsAsync();

            await Task.WhenAll(profileTask, recommendedModulesTask, allModulesTask, pathsTask);

            var profile = await profileTask;
            var recommendedModules = await recommendedModulesTask;
            var allModules = await allModulesTask;
            var paths = await pathsTask;

            return Ok(new
            {
                success = true,
                data = new
                {
                    learning_stats = new
                    {
                        total_learning_minutes = profile.TotalLearningMinutes,
                        completed_modules = profile.CompletedModules.Count,
                        completed_paths = profile.CompletedPaths.Count,
                        current_streak_days = CalculateLearningStreak(profile.LastLearningActivity),
                        experience_level = profile.Experience
                    },
                    progress_overview = profile.CategoryProgress,
                    recommended_modules = recommendedModules.Take(3).Select(m => new
                    {
                        id = m.Id,
                        title = m.Title,
                        category = m.Category,
                        difficulty = m.Difficulty,
                        estimated_minutes = m.EstimatedMinutes,
                        thumbnail_url = m.ThumbnailUrl
                    }),
                    popular_categories = allModules
                        .GroupBy(m => m.Category)
                        .Select(g => new
                        {
                            category = g.Key,
                            module_count = g.Count(),
                            avg_difficulty = g.Select(m => m.Difficulty).FirstOrDefault()
                        })
                        .Take(4),
                    featured_paths = paths
                        .Where(p => p.IsRecommended)
                        .Take(2)
                        .Select(p => new
                        {
                            id = p.Id,
                            name = p.Name,
                            description = p.Description,
                            estimated_hours = p.EstimatedHours,
                            thumbnail_url = p.ThumbnailUrl
                        }),
                    generated_at = DateTimeOffset.UtcNow
                }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Failed to get education dashboard" });
        }
    }

    // Helper method to calculate learning streak
    private int CalculateLearningStreak(DateTimeOffset lastActivity)
    {
        var daysSinceActivity = (DateTimeOffset.UtcNow - lastActivity).TotalDays;
        return daysSinceActivity <= 1 ? (int)Math.Max(0, 7 - daysSinceActivity) : 0; // Mock streak calculation
    }
}