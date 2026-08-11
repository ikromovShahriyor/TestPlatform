using TestPlatform.Domain.Configuration;

namespace TestPlatform.Domain.Entities;

public class TestAttempt : Auditable
{
    public Guid TestId { get; set; }
    public Test Test { get; set; } = null!;

    public string StudentName { get; set; } = string.Empty;
    public int TotalScore { get; set; }
    public int EarnedScore { get; set; }
    public decimal Percentage { get; set; }
    public DateTime PassedAt { get; set; }

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SubmittedAt { get; set; }
    public int DurationSeconds { get; set; }
    public bool IsExpired { get; set; }

    // Relationship to User (optional)
    public Guid? UserId { get; set; }
    public User? User { get; set; }

    public ICollection<StudentAnswer> Answers { get; set; } = new List<StudentAnswer>();
}