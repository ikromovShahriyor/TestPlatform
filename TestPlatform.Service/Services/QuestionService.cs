using Microsoft.EntityFrameworkCore;
using TestPlatform.Data.Repositories;
using TestPlatform.Domain.Entities;
using TestPlatform.Service.DTOs.Questions;
using TestPlatform.Service.Interfaces;

namespace TestPlatform.Service.Services;

public class QuestionService : IQuestionService
{
    private readonly IRepository<Test> _testRepository;
    private readonly IRepository<Question> _questionRepository;

    public QuestionService(
        IRepository<Test> testRepository,
        IRepository<Question> questionRepository)
    {
        _testRepository = testRepository;
        _questionRepository = questionRepository;
    }

    public async Task<QuestionResultDto> CreateAsync(Guid testId, QuestionCreateDto dto)
    {
        // 1. Test mavjudligini tekshirish
        var test = await _testRepository.GetAsync(t => t.Id == testId);
        if (test == null)
            throw new Exception("Test topilmadi!");

        // 2. Biznes qoida: Aynan 4 ta variant bo'lishi shart
        if (dto.Options.Count != 4)
            throw new Exception("Savolda aynan 4 ta variant bo‘lishi kerak!");

        // 3. Biznes qoida: Faqat bitta to'g'ri variant bo'lishi shart
        if (dto.Options.Count(o => o.IsCorrect) != 1)
            throw new Exception("Variantlardan faqat bittasi to‘g‘ri deb belgilanishi shart!");

        // 4. Map qilish va bazaga saqlash
        var question = new Question
        {
            TestId = testId,
            Text = dto.Text,
            Points = dto.Points,
            Options = dto.Options.Select(o => new AnswerOption
            {
                Text = o.Text,
                IsCorrect = o.IsCorrect
            }).ToList()
        };

        await _questionRepository.AddAsync(question);
        await _questionRepository.SaveChangesAsync();

        return MapToResultDto(question);
    }

    public async Task<IEnumerable<QuestionResultDto>> CreateBulkAsync(Guid testId, IEnumerable<QuestionCreateDto> dtos)
    {
        var test = await _testRepository.GetAsync(t => t.Id == testId);
        if (test == null)
            throw new Exception("Test topilmadi!");

        var results = new List<QuestionResultDto>();
        foreach (var dto in dtos)
        {
            if (dto.Options.Count != 4)
                throw new Exception($"'{dto.Text}' savolida aynan 4 ta variant bo'lishi kerak!");

            if (dto.Options.Count(o => o.IsCorrect) != 1)
                throw new Exception($"'{dto.Text}' savolida faqat bitta to'g'ri variant bo'lishi kerak!");

            var question = new Question
            {
                TestId = testId,
                Text = dto.Text,
                Points = dto.Points,
                Options = dto.Options.Select(o => new AnswerOption
                {
                    Text = o.Text,
                    IsCorrect = o.IsCorrect
                }).ToList()
            };

            await _questionRepository.AddAsync(question);
            results.Add(MapToResultDto(question));
        }

        await _questionRepository.SaveChangesAsync();
        return results;
    }

    public async Task<ImportResultDto> ImportQuestionsAsync(Guid testId, IEnumerable<QuestionCreateDto> dtos)
    {
        var test = await _testRepository.GetAsync(t => t.Id == testId);
        if (test == null)
            throw new Exception("Test topilmadi!");

        if (test.IsPublished)
            throw new Exception("Nashr qilingan (Published) testga savollar import qilish taqiqlanadi!");

        var result = new ImportResultDto
        {
            TotalRows = dtos.Count()
        };

        int index = 1;
        foreach (var dto in dtos)
        {
            if (string.IsNullOrWhiteSpace(dto.Text))
            {
                result.Errors.Add($"{index}-qator: Savol matni bo'sh bo'lishi mumkin emas.");
                result.FailedCount++;
                index++;
                continue;
            }

            if (dto.Options == null || dto.Options.Count != 4)
            {
                result.Errors.Add($"{index}-qator ('{dto.Text}'): Savolda aynan 4 ta variant bo'lishi kerak.");
                result.FailedCount++;
                index++;
                continue;
            }

            if (dto.Options.Count(o => o.IsCorrect) != 1)
            {
                result.Errors.Add($"{index}-qator ('{dto.Text}'): Variantlardan aynan 1 tasi to'g'ri deb belgilanishi shart.");
                result.FailedCount++;
                index++;
                continue;
            }

            var question = new Question
            {
                TestId = testId,
                Text = dto.Text,
                Points = dto.Points > 0 ? dto.Points : 10,
                Options = dto.Options.Select(o => new AnswerOption
                {
                    Text = o.Text,
                    IsCorrect = o.IsCorrect
                }).ToList()
            };

            await _questionRepository.AddAsync(question);
            result.ImportedCount++;
            index++;
        }

        if (result.ImportedCount > 0)
        {
            await _questionRepository.SaveChangesAsync();
        }

        return result;
    }

    public async Task<QuestionResultDto> UpdateAsync(Guid id, QuestionUpdateDto dto)
    {
        var question = await _questionRepository.GetAll(q => q.Id == id)
            .Include(q => q.Options)
            .FirstOrDefaultAsync();

        if (question == null)
            throw new Exception("Savol topilmadi!");

        if (dto.Options.Count != 4)
            throw new Exception("Savolda aynan 4 ta variant bo‘lishi kerak!");

        if (dto.Options.Count(o => o.IsCorrect) != 1)
            throw new Exception("Variantlardan faqat bittasi to‘g‘ri bo‘lishi shart!");

        question.Text = dto.Text;
        question.Points = dto.Points;

        int index = 0;
        foreach (var optionEntity in question.Options)
        {
            var optionDto = dto.Options[index++];
            optionEntity.Text = optionDto.Text;
            optionEntity.IsCorrect = optionDto.IsCorrect;
        }

        _questionRepository.Update(question);
        await _questionRepository.SaveChangesAsync();

        return MapToResultDto(question);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var question = await _questionRepository.GetAsync(q => q.Id == id);
        if (question == null)
            throw new Exception("Savol topilmadi!");

        _questionRepository.Delete(question);
        await _questionRepository.SaveChangesAsync();
        return true;
    }

    private QuestionResultDto MapToResultDto(Question question)
    {
        return new QuestionResultDto
        {
            Id = question.Id,
            Text = question.Text,
            Points = question.Points,
            Options = question.Options.Select(o => new AnswerOptionResultDto
            {
                Id = o.Id,
                Text = o.Text
            }).ToList()
        };
    }
}