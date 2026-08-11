using System.ComponentModel.DataAnnotations;

namespace TestPlatform.Service.DTOs.Questions;

public class QuestionCreateDto
{
    [Required]
    [StringLength(500)]
    public string Text { get; set; } = string.Empty;

    [Range(1, 100)]
    public int Points { get; set; } = 1;

    [Required]
    public List<AnswerOptionCreateDto> Options { get; set; } = new();
}

public class QuestionUpdateDto
{
    [Required]
    [StringLength(500)]
    public string Text { get; set; } = string.Empty;

    [Range(1, 100)]
    public int Points { get; set; }

    [Required]
    public List<AnswerOptionUpdateDto> Options { get; set; } = new();
}

public class QuestionResultDto
{
    public Guid Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public int Points { get; set; }
    public List<AnswerOptionResultDto> Options { get; set; } = new();
}