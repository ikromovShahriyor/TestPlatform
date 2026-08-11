using System;
using System.Threading.Tasks;
using TestPlatform.Service.DTOs.Certificates;

namespace TestPlatform.Service.Interfaces;

public interface ICertificateService
{
    Task<CertificateDetailsDto> GenerateCertificateAsync(Guid attemptId);
    Task<CertificateDetailsDto?> GetByNumberAsync(string certificateNumber);
    string GenerateCertificateSvg(CertificateDetailsDto dto);
}
