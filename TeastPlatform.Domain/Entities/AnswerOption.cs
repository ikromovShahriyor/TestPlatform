using System.ComponentModel.DataAnnotations;
using TestPlatform.Domain.Configuration;

namespace TestPlatform.Domain.Entities;

public class AnswerOption : Auditable
{
    [Required]
    [MaxLength(300)]
    public string Text { get; set; } = string.Empty;

    public bool IsCorrect { get; set; }

    // Foreign Key
    public Guid QuestionId { get; set; }

    // Navigation Properties
    public Question Question { get; set; } = null!;
}