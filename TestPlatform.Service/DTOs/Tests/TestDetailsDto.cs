namespace TestPlatform.Service.DTOs.Tests;

public class TestDetailsDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int PassingPercentage { get; set; }
    public int DurationMinutes { get; set; } = 15;
    public int TimeLimitMinutes { get; set; } = 15;
    public bool IsPublished { get; set; }
    public int? MaxAttemptsPerStudent { get; set; }
    public bool ShowReviewAfterSubmit { get; set; }
    public string Difficulty { get; set; } = "Medium";
    public List<string> Topics { get; set; } = new List<string>();
    public ICollection<QuestionItemDto> Questions { get; set; } = new List<QuestionItemDto>();
}

public class QuestionItemDto
{
    public Guid Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public int Points { get; set; }
    public ICollection<OptionItemDto> Options { get; set; } = new List<OptionItemDto>();
}

public class OptionItemDto
{
    public Guid Id { get; set; }
    public string Text { get; set; } = string.Empty;
}