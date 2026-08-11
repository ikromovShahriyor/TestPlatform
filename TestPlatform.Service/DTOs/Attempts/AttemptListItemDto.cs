namespace TestPlatform.Service.DTOs.Attempts;

public class AttemptListItemDto
{
    public Guid Id { get; set; }
    public Guid TestId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public int CorrectAnswersCount { get; set; }
    public int TotalQuestions { get; set; }
    public int EarnedScore { get; set; }
    public int TotalScore { get; set; }
    public double Percentage { get; set; }
    public bool IsPassed { get; set; }
    public DateTime PassedAt { get; set; }
    public bool IsExpired { get; set; }
    public int DurationSeconds { get; set; }
}
