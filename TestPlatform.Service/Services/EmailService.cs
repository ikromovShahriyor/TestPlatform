using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Mail;
using TestPlatform.Service.Interfaces;

namespace TestPlatform.Service.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<bool> SendResultEmailAsync(string toEmail, string studentName, string testTitle, int score, int totalScore, double percentage, bool isPassed)
    {
        if (string.IsNullOrWhiteSpace(toEmail) || !toEmail.Contains("@"))
        {
            _logger.LogWarning($"Email address invalid: '{toEmail}'");
            return false;
        }

        try
        {
            var host = _configuration["Email:SmtpHost"] ?? "smtp.gmail.com";
            var portStr = _configuration["Email:SmtpPort"] ?? "587";
            var senderEmail = _configuration["Email:SenderEmail"] ?? "no-reply@testplatform.uz";
            var password = _configuration["Email:SenderPassword"] ?? "";

            // If credentials are not configured, simulate log sending cleanly without throwing
            if (string.IsNullOrEmpty(password))
            {
                _logger.LogInformation($"[Email Simulation] To: {toEmail} | Subject: Test Natijangiz ({testTitle}) | Status: {(isPassed ? "PASS" : "FAIL")} ({percentage}%)");
                return true;
            }

            int port = int.TryParse(portStr, out var p) ? p : 587;

            using var message = new MailMessage();
            message.From = new MailAddress(senderEmail, "TestPlatform Tizimi");
            message.To.Add(new MailAddress(toEmail));
            message.Subject = $"🎯 Test Natijangiz: {testTitle}";
            message.IsBodyHtml = true;

            var statusBadge = isPassed ? "<span style='color:#10b981; font-weight:bold;'>Muvaffaqiyatli O'tdingiz 🎉</span>" : "<span style='color:#ef4444; font-weight:bold;'>Muvaffaqiyatsiz ❌</span>";

            message.Body = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; border: 1px solid #e2e8f0; border-radius: 8px; padding: 20px;'>
                    <h2 style='color: #0b1329;'>TestPlatform Natijalar Bildirishnomasi</h2>
                    <p>Salom <b>{studentName}</b>,</p>
                    <p>Siz <b>{testTitle}</b> bo'yicha imtihonni topshirdingiz.</p>
                    <table style='width: 100%; border-collapse: collapse; margin: 20px 0;'>
                        <tr><td style='padding: 8px; border-bottom: 1px solid #edf2f7;'>Natija Holati:</td><td style='padding: 8px; border-bottom: 1px solid #edf2f7;'>{statusBadge}</td></tr>
                        <tr><td style='padding: 8px; border-bottom: 1px solid #edf2f7;'>Toplangan Ball:</td><td style='padding: 8px; border-bottom: 1px solid #edf2f7;'>{score} / {totalScore}</td></tr>
                        <tr><td style='padding: 8px; border-bottom: 1px solid #edf2f7;'>Foiz Ko'rsatkichi:</td><td style='padding: 8px; border-bottom: 1px solid #edf2f7;'>{percentage}%</td></tr>
                    </table>
                    <p style='color: #64748b; font-size: 0.9em;'>TestPlatform tizimidan avtomatik yuborildi.</p>
                </div>";

            using var client = new SmtpClient(host, port)
            {
                Credentials = new NetworkCredential(senderEmail, password),
                EnableSsl = true
            };

            await client.SendMailAsync(message);
            _logger.LogInformation($"Result email successfully sent to {toEmail}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to send email to {toEmail}");
            return false;
        }
    }

    public async Task<bool> SendVerificationCodeAsync(string toEmail, string code)
    {
        if (string.IsNullOrWhiteSpace(toEmail) || !toEmail.Contains("@"))
        {
            _logger.LogWarning($"Email address invalid: '{toEmail}'");
            return false;
        }

        try
        {
            var host = _configuration["Email:SmtpHost"] ?? "smtp.gmail.com";
            var portStr = _configuration["Email:SmtpPort"] ?? "587";
            var senderEmail = _configuration["Email:SenderEmail"] ?? "no-reply@testplatform.uz";
            var password = _configuration["Email:SenderPassword"] ?? "";

            // If credentials are not configured in appsettings/env, simulate log sending cleanly
            if (string.IsNullOrEmpty(password))
            {
                _logger.LogInformation($"[Gmail OTP Simulation] To: {toEmail} | Verification Code: {code}");
                return true;
            }

            int port = int.TryParse(portStr, out var p) ? p : 587;

            using var message = new MailMessage();
            message.From = new MailAddress(senderEmail, "TestPlatform Xavfsizlik Xizmati");
            message.To.Add(new MailAddress(toEmail));
            message.Subject = $"🔑 TestPlatform Registratsiya Kodingiz: {code}";
            message.IsBodyHtml = true;

            message.Body = $@"
                <div style='font-family: Arial, sans-serif; max-width: 500px; margin: 0 auto; border: 1px solid #e2e8f0; border-radius: 12px; padding: 24px; background-color: #ffffff;'>
                    <h2 style='color: #4f46e5; text-align: center; margin-bottom: 20px;'>TestPlatform Tasdiqlash Kodi</h2>
                    <p style='color: #334155; font-size: 15px;'>Assalomu alaykum!</p>
                    <p style='color: #334155; font-size: 15px;'>TestPlatform tizimida ro'yxatdan o'tish uchun quyidagi 6 xonali tasdiqlash kodidan foydalaning:</p>
                    
                    <div style='background: #f1f5f9; border-radius: 8px; padding: 16px; text-align: center; margin: 24px 0;'>
                        <span style='font-size: 32px; font-weight: bold; letter-spacing: 6px; color: #0f172a;'>{code}</span>
                    </div>

                    <p style='color: #64748b; font-size: 13px;'>Ushbu kod <b>10 daqiqa</b> davomida amal qiladi. Agar siz ro'yxatdan o'tish so'rovini yubormagan bo'lsangiz, ushbu xabarni e'tiborsiz qoldiring.</p>
                </div>";

            using var client = new SmtpClient(host, port)
            {
                Credentials = new NetworkCredential(senderEmail, password),
                EnableSsl = true
            };

            await client.SendMailAsync(message);
            _logger.LogInformation($"OTP email successfully sent to {toEmail}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to send OTP email to {toEmail}");
            return false;
        }
    }
}
