using TestPlatform.Service.DTOs.Attempts;

namespace TestPlatform.Service.Interfaces;

public interface IAttemptService
{
    Task<AttemptResultDto> CreateAsync(AttemptCreateDto dto);
    Task<Guid> StartAttemptAsync(Guid testId, Guid userId, string studentName);
    Task<AttemptResultDto> SubmitAttemptAsync(Guid attemptId, List<StudentAnswerDto> answers);
    Task<AttemptReviewDto?> GetReviewAsync(Guid attemptId, Guid userId, string role);
    Task<AttemptResultDto?> GetByIdAsync(Guid id);
    Task<IEnumerable<AttemptListItemDto>> GetByTestIdAsync(Guid testId);
    Task<IEnumerable<AttemptListItemDto>> GetAllAsync();
}