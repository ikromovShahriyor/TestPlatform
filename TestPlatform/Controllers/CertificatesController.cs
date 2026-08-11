using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using TestPlatform.Service.Interfaces;

namespace TestPlatform.WebApi.Controllers;

[ApiController]
[Route("api")]
public class CertificatesController : ControllerBase
{
    private readonly ICertificateService _certificateService;

    public CertificatesController(ICertificateService certificateService)
    {
        _certificateService = certificateService;
    }

    [HttpPost("attempts/{attemptId:guid}/certificate")]
    [Authorize]
    public async Task<IActionResult> GenerateCertificate(Guid attemptId)
    {
        try
        {
            var result = await _certificateService.GenerateCertificateAsync(attemptId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("certificates/{certificateNumber}")]
    public async Task<IActionResult> GetByNumber(string certificateNumber)
    {
        var cert = await _certificateService.GetByNumberAsync(certificateNumber);
        if (cert == null) return NotFound(new { message = "Sertifikat topilmadi!" });

        return Ok(cert);
    }

    [HttpGet("certificates/{certificateNumber}/download")]
    public async Task<IActionResult> DownloadSvg(string certificateNumber)
    {
        var cert = await _certificateService.GetByNumberAsync(certificateNumber);
        if (cert == null) return NotFound("Sertifikat topilmadi!");

        var svgContent = _certificateService.GenerateCertificateSvg(cert);
        var bytes = Encoding.UTF8.GetBytes(svgContent);

        return File(bytes, "image/svg+xml", $"Certificate_{certificateNumber}.svg");
    }
}
