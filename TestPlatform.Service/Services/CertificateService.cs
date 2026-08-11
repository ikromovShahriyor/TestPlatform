using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestPlatform.Data.Context;
using TestPlatform.Domain.Entities;
using TestPlatform.Service.DTOs.Certificates;
using TestPlatform.Service.Interfaces;

namespace TestPlatform.Service.Services;

public class CertificateService : ICertificateService
{
    private readonly AppDbContext _context;

    public CertificateService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<CertificateDetailsDto> GenerateCertificateAsync(Guid attemptId)
    {
        var attempt = await _context.TestAttempts
            .Include(a => a.Test)
            .FirstOrDefaultAsync(a => a.Id == attemptId);

        if (attempt == null) throw new Exception("Urinish topilmadi!");
        if (attempt.SubmittedAt == null) throw new Exception("Urinish hali topshirilmagan!");

        var test = attempt.Test;
        if (test == null) throw new Exception("Test topilmadi!");

        double percentage = (double)attempt.Percentage;
        if (percentage < test.PassingPercentage)
        {
            throw new Exception("Urinish muvaffaqiyatsiz bo'lganligi sababli sertifikat berilmaydi!");
        }

        // Check if already exists
        var existing = await _context.Certificates.FirstOrDefaultAsync(c => c.AttemptId == attemptId);
        if (existing != null)
        {
            return new CertificateDetailsDto
            {
                CertificateNumber = existing.CertificateNumber,
                StudentName = existing.StudentName,
                TestTitle = existing.TestTitle,
                Percentage = percentage,
                IssuedAt = existing.IssuedAt
            };
        }

        // Generate unique number: TP-YYYYMMDD-XXXX
        var issuedAt = DateTime.UtcNow;
        var shortGuid = Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper();
        var certNumber = $"TP-{issuedAt:yyyyMMdd}-{shortGuid}";

        var cert = new Certificate
        {
            AttemptId = attemptId,
            CertificateNumber = certNumber,
            IssuedAt = issuedAt,
            StudentName = attempt.StudentName,
            TestTitle = test.Title
        };

        _context.Certificates.Add(cert);
        await _context.SaveChangesAsync();

        return new CertificateDetailsDto
        {
            CertificateNumber = cert.CertificateNumber,
            StudentName = cert.StudentName,
            TestTitle = cert.TestTitle,
            Percentage = percentage,
            IssuedAt = cert.IssuedAt
        };
    }

    public async Task<CertificateDetailsDto?> GetByNumberAsync(string certificateNumber)
    {
        var cert = await _context.Certificates
            .Include(c => c.Attempt)
            .FirstOrDefaultAsync(c => c.CertificateNumber.ToUpper() == certificateNumber.ToUpper().Trim());

        if (cert == null) return null;

        return new CertificateDetailsDto
        {
            CertificateNumber = cert.CertificateNumber,
            StudentName = cert.StudentName,
            TestTitle = cert.TestTitle,
            Percentage = cert.Attempt != null ? (double)cert.Attempt.Percentage : 100.0,
            IssuedAt = cert.IssuedAt
        };
    }

    public string GenerateCertificateSvg(CertificateDetailsDto dto)
    {
        var sb = new StringBuilder();
        sb.Append("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"800\" height=\"600\" viewBox=\"0 0 800 600\">");
        
        // Background definitions
        sb.Append("<defs>");
        sb.Append("<linearGradient id=\"bgGrad\" x1=\"0%\" y1=\"0%\" x2=\"100%\" y2=\"100%\">");
        sb.Append("<stop offset=\"0%\" stop-color=\"#0b1329\"/>");
        sb.Append("<stop offset=\"100%\" stop-color=\"#1c2541\"/>");
        sb.Append("</linearGradient>");
        sb.Append("<linearGradient id=\"goldGrad\" x1=\"0%\" y1=\"0%\" x2=\"100%\" y2=\"0%\">");
        sb.Append("<stop offset=\"0%\" stop-color=\"#d4af37\"/>");
        sb.Append("<stop offset=\"50%\" stop-color=\"#f9e8a2\"/>");
        sb.Append("<stop offset=\"100%\" stop-color=\"#aa7c11\"/>");
        sb.Append("</linearGradient>");
        sb.Append("</defs>");

        // Background Rect
        sb.Append("<rect width=\"800\" height=\"600\" fill=\"url(#bgGrad)\"/>");

        // Elegant Border
        sb.Append("<rect x=\"30\" y=\"30\" width=\"740\" height=\"540\" fill=\"none\" stroke=\"url(#goldGrad)\" stroke-width=\"4\" rx=\"15\"/>");
        sb.Append("<rect x=\"40\" y=\"40\" width=\"720\" height=\"520\" fill=\"none\" stroke=\"#10b981\" stroke-dasharray=\"10 5\" stroke-width=\"1\" rx=\"12\" opacity=\"0.6\"/>");

        // Corner Ornaments
        sb.Append("<path d=\"M 30 70 L 70 30 M 30 80 L 80 30\" stroke=\"url(#goldGrad)\" stroke-width=\"2\"/>");
        sb.Append("<path d=\"M 770 70 L 730 30 M 770 80 L 720 30\" stroke=\"url(#goldGrad)\" stroke-width=\"2\"/>");
        sb.Append("<path d=\"M 30 530 L 70 570 M 30 520 L 80 570\" stroke=\"url(#goldGrad)\" stroke-width=\"2\"/>");
        sb.Append("<path d=\"M 770 530 L 730 570 M 770 520 L 720 570\" stroke=\"url(#goldGrad)\" stroke-width=\"2\"/>");

        // Brand Title
        sb.Append("<text x=\"400\" y=\"110\" font-family=\"'Outfit', 'Inter', sans-serif\" font-size=\"28\" font-weight=\"800\" fill=\"url(#goldGrad)\" text-anchor=\"middle\" letter-spacing=\"2\">TESTPLATFORM SERTIFIKATI</text>");

        // Divider
        sb.Append("<line x1=\"250\" y1=\"135\" x2=\"550\" y2=\"135\" stroke=\"#10b981\" stroke-width=\"2\" opacity=\"0.8\"/>");
        sb.Append("<circle cx=\"400\" cy=\"135\" r=\"5\" fill=\"url(#goldGrad)\"/>");

        // Certificate body text
        sb.Append("<text x=\"400\" y=\"190\" font-family=\"'Inter', sans-serif\" font-size=\"16\" fill=\"#93c5fd\" text-anchor=\"middle\">Ushbu hujjat bilim darajasini muvaffaqiyatli tasdiqlaganligi uchun</text>");

        // Student Name (Highlight)
        sb.Append($"<text x=\"400\" y=\"260\" font-family=\"'Outfit', sans-serif\" font-size=\"36\" font-weight=\"800\" fill=\"#ffffff\" text-anchor=\"middle\" style=\"text-transform: uppercase; text-shadow: 0 4px 10px rgba(255,255,255,0.1);\">{dto.StudentName}</text>");

        sb.Append("<text x=\"400\" y=\"310\" font-family=\"'Inter', sans-serif\" font-size=\"16\" fill=\"#93c5fd\" text-anchor=\"middle\">shaxsiga topshirildi. U quyidagi fan bo'yicha imtihondan o'tdi:</text>");

        // Test Title
        sb.Append($"<text x=\"400\" y=\"360\" font-family=\"'Outfit', sans-serif\" font-size=\"22\" font-weight=\"700\" fill=\"#34d399\" text-anchor=\"middle\">{dto.TestTitle}</text>");

        // Result Percentage
        sb.Append($"<text x=\"400\" y=\"410\" font-family=\"'Inter', sans-serif\" font-size=\"18\" font-weight=\"600\" fill=\"#fbbf24\" text-anchor=\"middle\">Natija ko'rsatkichi: {dto.Percentage}%</text>");

        // Date and Number info columns
        var formattedDate = dto.IssuedAt.ToString("dd.MM.yyyy");
        sb.Append($"<text x=\"150\" y=\"480\" font-family=\"'Inter', sans-serif\" font-size=\"14\" fill=\"#94a3b8\">Sana: {formattedDate}</text>");
        sb.Append($"<text x=\"650\" y=\"480\" font-family=\"'Inter', sans-serif\" font-size=\"14\" fill=\"#94a3b8\" text-anchor=\"end\">Sertifikat: {dto.CertificateNumber}</text>");

        // Footer lines
        sb.Append("<line x1=\"100\" y1=\"515\" x2=\"250\" y2=\"515\" stroke=\"#475569\" stroke-width=\"1\"/>");
        sb.Append("<line x1=\"550\" y1=\"515\" x2=\"700\" y2=\"515\" stroke=\"#475569\" stroke-width=\"1\"/>");
        sb.Append("<text x=\"175\" y=\"535\" font-family=\"'Inter', sans-serif\" font-size=\"12\" fill=\"#64748b\" text-anchor=\"middle\">Berilgan sana</text>");
        sb.Append("<text x=\"625\" y=\"535\" font-family=\"'Inter', sans-serif\" font-size=\"12\" fill=\"#64748b\" text-anchor=\"middle\">Imzo / Tasdiq</text>");

        // Hologram seal simulation
        sb.Append("<circle cx=\"400\" cy=\"500\" r=\"35\" fill=\"none\" stroke=\"url(#goldGrad)\" stroke-width=\"2\" opacity=\"0.5\"/>");
        sb.Append("<path d=\"M 380 500 L 400 480 L 420 500 L 400 520 Z\" fill=\"url(#goldGrad)\" opacity=\"0.4\"/>");
        sb.Append("<text x=\"400\" y=\"548\" font-family=\"'Inter', sans-serif\" font-size=\"9\" fill=\"#d4af37\" text-anchor=\"middle\" opacity=\"0.8\">VERIFIED</text>");

        sb.Append("</svg>");
        return sb.ToString();
    }
}
