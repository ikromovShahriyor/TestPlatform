namespace TestPlatform.Service.Interfaces;

public interface IEmailService
{
    Task<bool> SendResultEmailAsync(string toEmail, string studentName, string testTitle, int score, int totalScore, double percentage, bool isPassed);
}
