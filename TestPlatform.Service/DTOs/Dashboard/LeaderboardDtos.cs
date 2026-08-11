using System;

namespace TestPlatform.Service.DTOs.Dashboard;

public class LeaderboardItemDto
{
    public int Rank { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public int Score { get; set; }
    public int TotalScore { get; set; }
    public double Percentage { get; set; }
    public int DurationSeconds { get; set; }
    public DateTime PassedAt { get; set; }
}

public class GlobalLeaderboardItemDto
{
    public int Rank { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public int AttemptsCount { get; set; }
    public double AveragePercentage { get; set; }
}
