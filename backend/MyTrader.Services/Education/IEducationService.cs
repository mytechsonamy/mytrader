using MyTrader.Core.DTOs.Education;

namespace MyTrader.Services.Education;

public interface IEducationService
{
    // Learning Modules
    Task<List<LearningModule>> GetLearningModulesAsync(string? category = null, string? difficulty = null);
    Task<LearningModule?> GetLearningModuleAsync(Guid moduleId, Guid? userId = null);
    Task<List<LearningPath>> GetLearningPathsAsync(string? category = null);
    Task<LearningPath?> GetLearningPathAsync(Guid pathId, Guid? userId = null);

    // Lessons and Content
    Task<LearningLesson?> GetLessonAsync(Guid lessonId, Guid userId);
    Task<bool> MarkLessonCompletedAsync(Guid lessonId, Guid userId);
    Task<List<LearningLesson>> GetLessonsForModuleAsync(Guid moduleId, Guid? userId = null);

    // Progress Tracking
    Task<LearningProgress?> GetModuleProgressAsync(Guid moduleId, Guid userId);
    Task<List<LearningProgress>> GetUserProgressAsync(Guid userId);
    Task<bool> UpdateProgressAsync(Guid userId, Guid moduleId, int timeSpentMinutes);
    Task<Dictionary<string, decimal>> GetCategoryProgressAsync(Guid userId);

    // Quizzes
    Task<Quiz?> GetQuizAsync(Guid quizId, Guid userId);
    Task<QuizResult> SubmitQuizAsync(Guid quizId, Guid userId, List<QuizAnswer> answers);
    Task<List<QuizResult>> GetQuizResultsAsync(Guid userId, Guid? quizId = null);

    // Interactive Tutorials
    Task<List<InteractiveTutorial>> GetTutorialsAsync(string? category = null, string? targetUrl = null);
    Task<InteractiveTutorial?> GetTutorialAsync(Guid tutorialId);
    Task<bool> MarkTutorialCompletedAsync(Guid tutorialId, Guid userId);
    Task<List<InteractiveTutorial>> GetRecommendedTutorialsAsync(Guid userId);

    // User Learning Profile
    Task<UserLearningProfile> GetLearningProfileAsync(Guid userId);
    Task<bool> UpdateLearningProfileAsync(Guid userId, UserLearningProfile profile);
    Task<List<LearningModule>> GetRecommendedModulesAsync(Guid userId, int limit = 5);
    Task<List<LearningPath>> GetRecommendedPathsAsync(Guid userId, int limit = 3);

    // Content Management
    Task<bool> BookmarkContentAsync(Guid userId, Guid contentId, string contentType);
    Task<bool> RemoveBookmarkAsync(Guid userId, Guid contentId);
    Task<List<object>> GetBookmarkedContentAsync(Guid userId);

    // Search and Discovery
    Task<List<object>> SearchContentAsync(string query, Guid? userId = null);
    Task<List<LearningModule>> GetPopularModulesAsync(int limit = 10);
    Task<List<string>> GetContentTagsAsync();
    Task<List<LearningModule>> GetModulesByTagsAsync(List<string> tags, Guid? userId = null);

    // Analytics
    Task<Dictionary<string, object>> GetLearningAnalyticsAsync(Guid userId);
    Task<Dictionary<string, int>> GetGlobalLearningStatsAsync();
}