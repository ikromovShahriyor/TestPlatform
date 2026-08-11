using System;

namespace TestPlatform.Service.DTOs.Certificates;

public class CertificateDetailsDto
{
    public string CertificateNumber { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
    public string TestTitle { get; set; } = string.Empty;
    public double Percentage { get; set; }
    public DateTime IssuedAt { get; set; }
}
