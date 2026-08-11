namespace TestPlatform.Service.DTOs.Attempts;

public class AttemptResultDto
{
    public Guid Id { get; set; }
    public Guid TestId { get; set; }
    public int TotalScore { get; set; }
    public int EarnedScore { get; set; }
    public double Percentage { get; set; }
    public DateTime PassedAt { get; set; }
    public bool IsExpired { get; set; }
    public int DurationSeconds { get; set; }
}