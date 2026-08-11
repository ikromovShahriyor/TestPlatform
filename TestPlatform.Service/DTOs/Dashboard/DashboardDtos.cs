using System;

namespace TestPlatform.Service.DTOs.Dashboard;

public class DashboardSummaryDto
{
    public int SubjectsCount { get; set; }
    public int TestsCount { get; set; }
    public int PublishedTestsCount { get; set; }
    public int QuestionsCount { get; set; }
    public int AttemptsCount { get; set; }
    public double AveragePercentage { get; set; }
    public int PassedAttemptsCount { get; set; }
    public int FailedAttemptsCount { get; set; }
}

public class TopTestDto
{
    public Guid TestId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string SubjectName { get; set; } = string.Empty;
    public int AttemptsCount { get; set; }
    public double AveragePercentage { get; set; }
}

public class RecentAttemptDto
{
    public Guid AttemptId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string TestTitle { get; set; } = string.Empty;
    public int EarnedScore { get; set; }
    public int TotalScore { get; set; }
    public double Percentage { get; set; }
    public bool IsPassed { get; set; }
    public DateTime PassedAt { get; set; }
}
