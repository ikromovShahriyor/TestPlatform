namespace TestPlatform.Domain.Configuration;

public abstract class Auditable
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}