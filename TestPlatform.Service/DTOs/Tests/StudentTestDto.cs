namespace TestPlatform.Service.DTOs.Tests;

public class StudentTestDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int PassingPercentage { get; set; }
    public int DurationMinutes { get; set; } = 15;
    public int TimeLimitMinutes { get; set; } = 15;
    public int? MaxAttemptsPerStudent { get; set; }
    public string Difficulty { get; set; } = "Medium";
    public List<string> Topics { get; set; } = new List<string>();
    public ICollection<QuestionItemDto> Questions { get; set; } = new List<QuestionItemDto>();
}