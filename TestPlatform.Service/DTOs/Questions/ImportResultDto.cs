namespace TestPlatform.Service.DTOs.Questions;

public class ImportResultDto
{
    public int TotalRows { get; set; }
    public int ImportedCount { get; set; }
    public int FailedCount { get; set; }
    public List<string> Errors { get; set; } = new List<string>();
}
