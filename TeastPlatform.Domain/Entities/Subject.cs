using TestPlatform.Domain.Configuration;

namespace TestPlatform.Domain.Entities;

public class Subject : Auditable
{
    public required string Name { get; set; }
    public string? Description { get; set; }

    // Munosabatlar (Navigation)
    public virtual ICollection<Test> Tests { get; set; } = new List<Test>();
}