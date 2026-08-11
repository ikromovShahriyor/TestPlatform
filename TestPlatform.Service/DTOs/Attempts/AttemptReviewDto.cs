namespace TestPlatform.Service.DTOs.Attempts;

public class AttemptReviewDto
{
    public Guid AttemptId { get; set; }
    public Guid TestId { get; set; }
    public string TestTitle { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
    public int TotalScore { get; set; }
    public int EarnedScore { get; set; }
    public double Percentage { get; set; }
    public bool IsPassed { get; set; }
    public DateTime PassedAt { get; set; }
    public bool IsExpired { get; set; }
    public int DurationSeconds { get; set; }

    public List<ReviewQuestionDto> Questions { get; set; } = new();
}

public class ReviewQuestionDto
{
    public Guid Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public int Points { get; set; }
    public Guid? SelectedOptionId { get; set; }
    public List<ReviewOptionDto> Options { get; set; } = new();
}

public class ReviewOptionDto
{
    public Guid Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
}
