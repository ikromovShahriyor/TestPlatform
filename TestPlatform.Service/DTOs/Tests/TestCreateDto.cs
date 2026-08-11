using System.ComponentModel.DataAnnotations;

namespace TestPlatform.Service.DTOs.Tests;

public class TestCreateDto
{
    [Required]
    public Guid SubjectId { get; set; }

    [Required]
    [MaxLength(150)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    [Range(1, 100)]
    public int PassingPercentage { get; set; }

    public int DurationMinutes { get; set; } = 15;

    [Range(1, 180, ErrorMessage = "Time limit must be between 1 and 180 minutes.")]
    public int TimeLimitMinutes { get; set; } = 15;

    public int? MaxAttemptsPerStudent { get; set; }
    public bool ShowReviewAfterSubmit { get; set; } = true;
    public bool IsPublished { get; set; } = true;
    public string Difficulty { get; set; } = "Medium";
    public List<Guid> TopicIds { get; set; } = new List<Guid>();
}