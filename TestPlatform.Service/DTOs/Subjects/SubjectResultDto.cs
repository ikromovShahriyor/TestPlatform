namespace TestPlatform.Service.DTOs.Subjects;

public class SubjectResultDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}