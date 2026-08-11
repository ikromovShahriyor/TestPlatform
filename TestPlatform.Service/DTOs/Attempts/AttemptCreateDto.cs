namespace TestPlatform.Service.DTOs.Attempts;

public class AttemptCreateDto
{
    public Guid TestId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public List<StudentAnswerDto> Answers { get; set; } = new();
}