using TestPlatform.Domain.Configuration;

namespace TestPlatform.Domain.Entities;

public class StudentAnswer : Auditable
{
    public bool IsCorrect { get; set; }
    public int EarnedPoints { get; set; }

    // Foreign Keys
    public Guid TestAttemptId { get; set; }
    public Guid QuestionId { get; set; }
    public Guid SelectedOptionId { get; set; }

    // Navigation Properties
    public TestAttempt TestAttempt { get; set; } = null!;
}