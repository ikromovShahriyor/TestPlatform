using TestPlatform.Service.DTOs.Questions;

namespace TestPlatform.Service.Interfaces;

public interface IQuestionService
{
    Task<QuestionResultDto> CreateAsync(Guid testId, QuestionCreateDto dto);
    Task<IEnumerable<QuestionResultDto>> CreateBulkAsync(Guid testId, IEnumerable<QuestionCreateDto> dtos);
    Task<ImportResultDto> ImportQuestionsAsync(Guid testId, IEnumerable<QuestionCreateDto> dtos);
    Task<QuestionResultDto> UpdateAsync(Guid id, QuestionUpdateDto dto);
    Task<bool> DeleteAsync(Guid id);
}