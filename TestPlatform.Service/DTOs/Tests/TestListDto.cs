namespace TestPlatform.Service.DTOs.Tests;

public class TestListDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string SubjectName { get; set; } = string.Empty;
    public int QuestionsCount { get; set; }
    public int PassingPercentage { get; set; }
    public int DurationMinutes { get; set; } = 15;
    public int TimeLimitMinutes { get; set; } = 15;
    public bool IsPublished { get; set; }
    public int? MaxAttemptsPerStudent { get; set; }
    public string Difficulty { get; set; } = "Medium";
    public List<string> Topics { get; set; } = new List<string>();
}