using System;
using System.ComponentModel.DataAnnotations;

namespace TestPlatform.Service.DTOs.Topics;

public class TopicCreateDto
{
    [Required(ErrorMessage = "Mavzu nomi kiritilishi shart.")]
    [StringLength(100, ErrorMessage = "Mavzu nomi 100 ta belgidan oshmasligi kerak.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Mavzu tavsifi 500 ta belgidan oshmasligi kerak.")]
    public string? Description { get; set; }
}

public class TopicUpdateDto
{
    [Required(ErrorMessage = "Mavzu nomi kiritilishi shart.")]
    [StringLength(100, ErrorMessage = "Mavzu nomi 100 ta belgidan oshmasligi kerak.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Mavzu tavsifi 500 ta belgidan oshmasligi kerak.")]
    public string? Description { get; set; }
}

public class TopicDetailsDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
}
