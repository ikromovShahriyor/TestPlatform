namespace TestPlatform.Service.DTOs.Attempts;

public class StudentAnswerDto
{
    public Guid QuestionId { get; set; }
    public Guid SelectedOptionId { get; set; }
}