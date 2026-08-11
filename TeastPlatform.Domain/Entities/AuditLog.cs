using System;
using TestPlatform.Domain.Configuration;

namespace TestPlatform.Domain.Entities;

public class AuditLog : Auditable
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public string Action { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
}
