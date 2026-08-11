using Microsoft.EntityFrameworkCore;
using TestPlatform.Data.Repositories;
using TestPlatform.Domain.Entities;
using TestPlatform.Service.DTOs.Attempts;
using TestPlatform.Service.Interfaces;

namespace TestPlatform.Service.Services;

public class AttemptService : IAttemptService
{
    private readonly IRepository<Test> _testRepository;
    private readonly IRepository<Question> _questionRepository;
    private readonly IRepository<TestAttempt> _attemptRepository;
    private readonly IRepository<User> _userRepository;
    private readonly IEmailService _emailService;

    public AttemptService(
        IRepository<Test> testRepository,
        IRepository<Question> questionRepository,
        IRepository<TestAttempt> attemptRepository,
        IRepository<User> userRepository,
        IEmailService emailService)
    {
        _testRepository = testRepository;
        _questionRepository = questionRepository;
        _attemptRepository = attemptRepository;
        _userRepository = userRepository;
        _emailService = emailService;
    }

    public async Task<AttemptResultDto> CreateAsync(AttemptCreateDto dto)
    {
        var test = await _testRepository.GetAsync(t => t.Id == dto.TestId);
        if (test == null)
            throw new Exception("Test topilmadi!");

        var questions = await _questionRepository.GetAll(q => q.TestId == dto.TestId, isTracking: false)
            .Include(q => q.Options)
            .ToListAsync();

        int totalScore = questions.Sum(q => q.Points);
        int earnedScore = 0;

        var studentAnswers = new List<StudentAnswer>();

        foreach (var answer in dto.Answers)
        {
            var question = questions.FirstOrDefault(q => q.Id == answer.QuestionId);
            if (question != null)
            {
                var correctOption = question.Options.FirstOrDefault(o => o.IsCorrect);
                bool isCorrect = correctOption != null && correctOption.Id == answer.SelectedOptionId;
                int earnedPoints = isCorrect ? question.Points : 0;

                if (isCorrect)
                {
                    earnedScore += question.Points;
                }

                studentAnswers.Add(new StudentAnswer
                {
                    QuestionId = answer.QuestionId,
                    SelectedOptionId = answer.SelectedOptionId,
                    IsCorrect = isCorrect,
                    EarnedPoints = earnedPoints
                });
            }
        }

        decimal percentage = totalScore > 0 ? Math.Round((decimal)earnedScore / totalScore * 100, 2) : 0;

        var attempt = new TestAttempt
        {
            TestId = dto.TestId,
            StudentName = string.IsNullOrWhiteSpace(dto.StudentName) ? "Talaba" : dto.StudentName,
            TotalScore = totalScore,
            EarnedScore = earnedScore,
            Percentage = percentage,
            PassedAt = DateTime.UtcNow,
            Answers = studentAnswers
        };

        await _attemptRepository.AddAsync(attempt);
        await _attemptRepository.SaveChangesAsync();

        return new AttemptResultDto
        {
            Id = attempt.Id,
            TestId = attempt.TestId,
            TotalScore = attempt.TotalScore,
            EarnedScore = attempt.EarnedScore,
            Percentage = (double)attempt.Percentage,
            PassedAt = attempt.PassedAt
        };
    }

    public async Task<Guid> StartAttemptAsync(Guid testId, Guid userId, string studentName)
    {
        var test = await _testRepository.GetAsync(t => t.Id == testId);
        if (test == null)
            throw new Exception("Test topilmadi!");

        if (test.MaxAttemptsPerStudent.HasValue && test.MaxAttemptsPerStudent.Value > 0)
        {
            var attemptsCount = await _attemptRepository.GetAll(a => a.TestId == testId && a.UserId == userId).CountAsync();
            if (attemptsCount >= test.MaxAttemptsPerStudent.Value)
            {
                throw new Exception($"Ushbu testni topshirish urinishlaringiz soni tugadi! Ruxsat etilgan urinishlar soni: {test.MaxAttemptsPerStudent.Value} ta.");
            }
        }

        var attempt = new TestAttempt
        {
            TestId = testId,
            UserId = userId,
            StudentName = string.IsNullOrWhiteSpace(studentName) ? "Talaba" : studentName,
            StartedAt = DateTime.UtcNow,
            PassedAt = DateTime.UtcNow // Required default passed date
        };

        await _attemptRepository.AddAsync(attempt);
        await _attemptRepository.SaveChangesAsync();

        return attempt.Id;
    }

    public async Task<AttemptResultDto> SubmitAttemptAsync(Guid attemptId, List<StudentAnswerDto> answers)
    {
        var attempt = await _attemptRepository.GetAll(a => a.Id == attemptId)
            .Include(a => a.Answers)
            .FirstOrDefaultAsync();

        if (attempt == null)
            throw new Exception("Urinish topilmadi!");

        if (attempt.SubmittedAt != null)
            throw new Exception("Urinish allaqachon topshirilgan!");

        var test = await _testRepository.GetAsync(t => t.Id == attempt.TestId);
        if (test == null)
            throw new Exception("Test topilmadi!");

        var questions = await _questionRepository.GetAll(q => q.TestId == attempt.TestId, isTracking: false)
            .Include(q => q.Options)
            .ToListAsync();

        int totalScore = questions.Sum(q => q.Points);
        int earnedScore = 0;

        var studentAnswers = new List<StudentAnswer>();

        foreach (var answer in answers)
        {
            var question = questions.FirstOrDefault(q => q.Id == answer.QuestionId);
            if (question != null)
            {
                var correctOption = question.Options.FirstOrDefault(o => o.IsCorrect);
                bool isCorrect = correctOption != null && correctOption.Id == answer.SelectedOptionId;
                int earnedPoints = isCorrect ? question.Points : 0;

                if (isCorrect)
                {
                    earnedScore += question.Points;
                }

                studentAnswers.Add(new StudentAnswer
                {
                    TestAttemptId = attempt.Id,
                    QuestionId = answer.QuestionId,
                    SelectedOptionId = answer.SelectedOptionId,
                    IsCorrect = isCorrect,
                    EarnedPoints = earnedPoints
                });
            }
        }

        var submittedAt = DateTime.UtcNow;
        var duration = (int)Math.Ceiling((submittedAt - attempt.StartedAt).TotalSeconds);

        // Time limit check: duration cannot exceed TimeLimitMinutes * 60 + 15 seconds grace period
        var limitMinutes = test.TimeLimitMinutes > 0 ? test.TimeLimitMinutes : test.DurationMinutes;
        var limitSeconds = limitMinutes * 60;
        bool isExpired = duration > (limitSeconds + 15);

        attempt.TotalScore = totalScore;
        attempt.EarnedScore = earnedScore;
        attempt.Percentage = totalScore > 0 ? Math.Round((decimal)attempt.EarnedScore / totalScore * 100, 2) : 0;
        attempt.PassedAt = submittedAt;
        attempt.SubmittedAt = submittedAt;
        attempt.DurationSeconds = duration;
        attempt.IsExpired = isExpired;

        if (attempt.Answers == null)
            attempt.Answers = new List<StudentAnswer>();
        else
            attempt.Answers.Clear();

        foreach (var sa in studentAnswers)
        {
            attempt.Answers.Add(sa);
        }

        _attemptRepository.Update(attempt);
        await _attemptRepository.SaveChangesAsync();

        // Send email notification safely
        if (attempt.UserId.HasValue)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    var user = await _userRepository.GetAsync(u => u.Id == attempt.UserId.Value);
                    if (user != null && !string.IsNullOrWhiteSpace(user.Email))
                    {
                        bool isPassed = (double)attempt.Percentage >= test.PassingPercentage;
                        await _emailService.SendResultEmailAsync(user.Email, user.FullName, test.Title, attempt.EarnedScore, attempt.TotalScore, (double)attempt.Percentage, isPassed);
                    }
                }
                catch
                {
                    // Fail-safe: Email errors do not affect submit result
                }
            });
        }

        return new AttemptResultDto
        {
            Id = attempt.Id,
            TestId = attempt.TestId,
            TotalScore = attempt.TotalScore,
            EarnedScore = attempt.EarnedScore,
            Percentage = (double)attempt.Percentage,
            PassedAt = attempt.PassedAt,
            IsExpired = attempt.IsExpired,
            DurationSeconds = attempt.DurationSeconds
        };
    }

    public async Task<AttemptReviewDto?> GetReviewAsync(Guid attemptId, Guid userId, string role)
    {
        var attempt = await _attemptRepository.GetAll(a => a.Id == attemptId)
            .Include(a => a.Answers)
            .Include(a => a.Test)
            .FirstOrDefaultAsync();

        if (attempt == null) return null;

        if (role != "Admin" && attempt.UserId != userId)
        {
            throw new UnauthorizedAccessException("Sizda ushbu urinish natijasini ko'rish huquqi yo'q!");
        }

        var test = attempt.Test;
        if (test == null)
            throw new Exception("Test topilmadi!");

        if (role != "Admin" && !test.ShowReviewAfterSubmit)
        {
            throw new Exception("Ushbu test uchun javoblarni tahlil qilish (Review) rejimi admin tomonidan o'chirilgan.");
        }

        var questions = await _questionRepository.GetAll(q => q.TestId == attempt.TestId, isTracking: false)
            .Include(q => q.Options)
            .ToListAsync();

        var review = new AttemptReviewDto
        {
            AttemptId = attempt.Id,
            TestId = attempt.TestId,
            TestTitle = test.Title,
            StudentName = attempt.StudentName,
            TotalScore = attempt.TotalScore,
            EarnedScore = attempt.EarnedScore,
            Percentage = (double)attempt.Percentage,
            IsPassed = (double)attempt.Percentage >= test.PassingPercentage,
            PassedAt = attempt.PassedAt,
            IsExpired = attempt.IsExpired,
            DurationSeconds = attempt.DurationSeconds
        };

        foreach (var q in questions)
        {
            var answer = attempt.Answers.FirstOrDefault(a => a.QuestionId == q.Id);
            review.Questions.Add(new ReviewQuestionDto
            {
                Id = q.Id,
                Text = q.Text,
                Points = q.Points,
                SelectedOptionId = answer?.SelectedOptionId,
                Options = q.Options.Select(o => new ReviewOptionDto
                {
                    Id = o.Id,
                    Text = o.Text,
                    IsCorrect = o.IsCorrect
                }).ToList()
            });
        }

        return review;
    }

    public async Task<AttemptResultDto?> GetByIdAsync(Guid id)
    {
        var attempt = await _attemptRepository.GetAsync(a => a.Id == id);
        if (attempt == null) return null;

        return new AttemptResultDto
        {
            Id = attempt.Id,
            TestId = attempt.TestId,
            TotalScore = attempt.TotalScore,
            EarnedScore = attempt.EarnedScore,
            Percentage = (double)attempt.Percentage,
            PassedAt = attempt.PassedAt,
            IsExpired = attempt.IsExpired,
            DurationSeconds = attempt.DurationSeconds
        };
    }

    public async Task<IEnumerable<AttemptListItemDto>> GetByTestIdAsync(Guid testId)
    {
        var test = await _testRepository.GetAsync(t => t.Id == testId);
        int passingPercentage = test?.PassingPercentage ?? 60;

        var attempts = await _attemptRepository.GetAll(a => a.TestId == testId, isTracking: false)
            .Include(a => a.Answers)
            .OrderByDescending(a => a.PassedAt)
            .ToListAsync();

        return attempts.Select(a => new AttemptListItemDto
        {
            Id = a.Id,
            TestId = a.TestId,
            StudentName = string.IsNullOrWhiteSpace(a.StudentName) ? "Talaba" : a.StudentName,
            CorrectAnswersCount = a.Answers.Count(ans => ans.IsCorrect),
            TotalQuestions = a.Answers.Count,
            EarnedScore = a.EarnedScore,
            TotalScore = a.TotalScore,
            Percentage = (double)a.Percentage,
            IsPassed = (double)a.Percentage >= passingPercentage,
            PassedAt = a.PassedAt,
            IsExpired = a.IsExpired,
            DurationSeconds = a.DurationSeconds
        });
    }

    public async Task<IEnumerable<AttemptListItemDto>> GetAllAsync()
    {
        var attempts = await _attemptRepository.GetAll(isTracking: false)
            .Include(a => a.Answers)
            .Include(a => a.Test)
            .OrderByDescending(a => a.PassedAt)
            .ToListAsync();

        return attempts.Select(a => new AttemptListItemDto
        {
            Id = a.Id,
            TestId = a.TestId,
            StudentName = string.IsNullOrWhiteSpace(a.StudentName) ? "Talaba" : a.StudentName,
            CorrectAnswersCount = a.Answers.Count(ans => ans.IsCorrect),
            TotalQuestions = a.Answers.Count,
            EarnedScore = a.EarnedScore,
            TotalScore = a.TotalScore,
            Percentage = (double)a.Percentage,
            IsPassed = (double)a.Percentage >= (a.Test != null ? a.Test.PassingPercentage : 60),
            PassedAt = a.PassedAt,
            IsExpired = a.IsExpired,
            DurationSeconds = a.DurationSeconds
        });
    }
}