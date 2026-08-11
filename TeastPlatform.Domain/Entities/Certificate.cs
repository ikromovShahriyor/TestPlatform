using System;
using TestPlatform.Domain.Configuration;

namespace TestPlatform.Domain.Entities;

public class Certificate : Auditable
{
    public Guid AttemptId { get; set; }
    public TestAttempt Attempt { get; set; } = null!;

    public string CertificateNumber { get; set; } = string.Empty;
    public DateTime IssuedAt { get; set; }
    
    public string StudentName { get; set; } = string.Empty;
    public string TestTitle { get; set; } = string.Empty;
}
