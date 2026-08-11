using System.ComponentModel.DataAnnotations;
using TestPlatform.Domain.Configuration;
using TestPlatform.Domain.Enums;

namespace TestPlatform.Domain.Entities;

public class Test : Auditable
{
    [Required]
    [MaxLength(150)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    [Range(1, 100)]
    public int PassingPercentage { get; set; }

    public int DurationMinutes { get; set; } = 15;

    [Range(1, 180)]
    public int TimeLimitMinutes { get; set; } = 15;

    public bool IsPublished { get; set; } = false;

    public int? MaxAttemptsPerStudent { get; set; } = null;
    public bool ShowReviewAfterSubmit { get; set; } = true;
    public DifficultyLevel Difficulty { get; set; } = DifficultyLevel.Medium;

    // Foreign Key
    public Guid SubjectId { get; set; }

    // Navigation Properties
    public Subject Subject { get; set; } = null!;
    public ICollection<Question> Questions { get; set; } = new List<Question>();
    public ICollection<TestAttempt> Attempts { get; set; } = new List<TestAttempt>();
    public ICollection<TestTopic> TestTopics { get; set; } = new List<TestTopic>();
}