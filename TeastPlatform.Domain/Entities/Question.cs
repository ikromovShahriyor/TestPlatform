using System.ComponentModel.DataAnnotations;
using TestPlatform.Domain.Configuration;

namespace TestPlatform.Domain.Entities;

public class Question : Auditable
{
    [Required]
    [MaxLength(500)]
    public string Text { get; set; } = string.Empty;

    [Range(1, 100)]
    public int Points { get; set; } = 1;

    // Foreign Key
    public Guid TestId { get; set; }

    // Navigation Properties
    public Test Test { get; set; } = null!;
    public ICollection<AnswerOption> Options { get; set; } = new List<AnswerOption>();
}