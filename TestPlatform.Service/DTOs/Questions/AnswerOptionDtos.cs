using System.ComponentModel.DataAnnotations;

namespace TestPlatform.Service.DTOs.Questions;

public class AnswerOptionCreateDto
{
    [Required]
    [StringLength(300)]
    public string Text { get; set; } = string.Empty;

    public bool IsCorrect { get; set; }
}

public class AnswerOptionUpdateDto
{
    public Guid Id { get; set; }

    [Required]
    [StringLength(300)]
    public string Text { get; set; } = string.Empty;

    public bool IsCorrect { get; set; }
}

public class AnswerOptionResultDto
{
    public Guid Id { get; set; }
    public string Text { get; set; } = string.Empty;
}