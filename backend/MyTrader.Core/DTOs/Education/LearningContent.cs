using System;
using System.Collections.Generic;

namespace MyTrader.Core.DTOs.Education;

public class LearningModule
{
    public Guid Id { get; set; }
    public string Title { get; set; } = default!;
    public string Description { get; set; } = default!;
    public string Category { get; set; } = default!; // "Basics", "Technical Analysis", "Risk Management", "Advanced"
    public string Difficulty { get; set; } = default!; // "Beginner", "Intermediate", "Advanced"
    public int EstimatedMinutes { get; set; }
    public string ThumbnailUrl { get; set; } = default!;
    public List<string> Tags { get; set; } = new();
    public List<LearningLesson> Lessons { get; set; } = new();
    public LearningProgress Progress { get; set; } = new();
    public int OrderIndex { get; set; }
    public bool IsPublished { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class LearningLesson
{
    public Guid Id { get; set; }
    public Guid ModuleId { get; set; }
    public string Title { get; set; } = default!;
    public string Content { get; set; } = default!; // Markdown content
    public string LessonType { get; set; } = default!; // "Reading", "Video", "Interactive", "Quiz"
    public int EstimatedMinutes { get; set; }
    public string? VideoUrl { get; set; }
    public List<string> ImageUrls { get; set; } = new();
    public InteractiveContent? Interactive { get; set; }
    public Quiz? Quiz { get; set; }
    public List<string> KeyPoints { get; set; } = new();
    public int OrderIndex { get; set; }
    public bool IsCompleted { get; set; }
}

public class InteractiveContent
{
    public string Type { get; set; } = default!; // "Chart", "Calculator", "Simulation"
    public Dictionary<string, object> Configuration { get; set; } = new();
    public string Instructions { get; set; } = default!;
    public List<InteractiveStep> Steps { get; set; } = new();
}

public class InteractiveStep
{
    public int StepNumber { get; set; }
    public string Title { get; set; } = default!;
    public string Description { get; set; } = default!;
    public string Action { get; set; } = default!; // "Click", "Input", "Select", "Draw"
    public Dictionary<string, object> Data { get; set; } = new();
    public bool IsCompleted { get; set; }
}

public class Quiz
{
    public Guid Id { get; set; }
    public string Title { get; set; } = default!;
    public string Description { get; set; } = default!;
    public List<QuizQuestion> Questions { get; set; } = new();
    public int PassingScore { get; set; } = 70; // Percentage
    public int MaxAttempts { get; set; } = 3;
    public int TimeLimit { get; set; } = 0; // 0 = no limit, in minutes
    public QuizResult? LastResult { get; set; }
}

public class QuizQuestion
{
    public Guid Id { get; set; }
    public string Question { get; set; } = default!;
    public string Type { get; set; } = default!; // "MultipleChoice", "TrueFalse", "FillInBlank"
    public List<string> Options { get; set; } = new();
    public List<int> CorrectAnswers { get; set; } = new(); // Indices of correct options
    public string Explanation { get; set; } = default!;
    public int Points { get; set; } = 1;
    public string? ImageUrl { get; set; }
}

public class QuizResult
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid QuizId { get; set; }
    public int Score { get; set; }
    public int TotalPoints { get; set; }
    public decimal Percentage { get; set; }
    public bool Passed { get; set; }
    public List<QuizAnswer> Answers { get; set; } = new();
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset CompletedAt { get; set; }
    public int AttemptNumber { get; set; }
}

public class QuizAnswer
{
    public Guid QuestionId { get; set; }
    public List<int> SelectedAnswers { get; set; } = new();
    public bool IsCorrect { get; set; }
    public int PointsEarned { get; set; }
}

public class LearningProgress
{
    public Guid UserId { get; set; }
    public Guid ModuleId { get; set; }
    public int CompletedLessons { get; set; }
    public int TotalLessons { get; set; }
    public decimal CompletionPercentage { get; set; }
    public int TimeSpentMinutes { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset LastAccessedAt { get; set; } = DateTimeOffset.UtcNow;
    public List<Guid> CompletedLessonIds { get; set; } = new();
    public Dictionary<string, object> CustomData { get; set; } = new();
}

public class LearningPath
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string Description { get; set; } = default!;
    public string Category { get; set; } = default!;
    public string Difficulty { get; set; } = default!;
    public List<Guid> ModuleIds { get; set; } = new();
    public List<LearningModule> Modules { get; set; } = new();
    public LearningProgress PathProgress { get; set; } = new();
    public string ThumbnailUrl { get; set; } = default!;
    public List<string> PrerequisitePaths { get; set; } = new();
    public int EstimatedHours { get; set; }
    public bool IsRecommended { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

// Tutorial specific models
public class TutorialStep
{
    public int StepNumber { get; set; }
    public string Title { get; set; } = default!;
    public string Description { get; set; } = default!;
    public string TargetElement { get; set; } = default!; // CSS selector
    public string Position { get; set; } = "bottom"; // "top", "bottom", "left", "right"
    public string Action { get; set; } = default!; // "Click", "Hover", "Input", "Highlight"
    public string? NextTrigger { get; set; } // What triggers next step
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class InteractiveTutorial
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string Description { get; set; } = default!;
    public string Category { get; set; } = default!; // "Getting Started", "Chart Analysis", "Trading"
    public List<TutorialStep> Steps { get; set; } = new();
    public string TriggerUrl { get; set; } = default!; // Where tutorial should appear
    public bool IsActive { get; set; } = true;
    public string TargetUserType { get; set; } = "Beginner"; // "Beginner", "All", "Advanced"
    public int Priority { get; set; } = 0; // Higher = more important
    public Dictionary<string, object> Configuration { get; set; } = new();
}

public class UserLearningProfile
{
    public Guid UserId { get; set; }
    public string LearningStyle { get; set; } = default!; // "Visual", "Auditory", "Kinesthetic", "Mixed"
    public List<string> Interests { get; set; } = new(); // "Technical Analysis", "Fundamental", "Crypto", etc.
    public string Experience { get; set; } = "Beginner"; // "Beginner", "Intermediate", "Advanced"
    public List<Guid> CompletedModules { get; set; } = new();
    public List<Guid> CompletedPaths { get; set; } = new();
    public List<Guid> BookmarkedContent { get; set; } = new();
    public Dictionary<string, int> CategoryProgress { get; set; } = new(); // Category -> percentage
    public int TotalLearningMinutes { get; set; }
    public DateTimeOffset LastLearningActivity { get; set; }
    public Dictionary<string, object> Preferences { get; set; } = new();
}