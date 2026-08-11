using TestPlatform.Service.DTOs.Tests;
using TestPlatform.Service.DTOs.Pagination;

namespace TestPlatform.Service.Interfaces;

public interface ITestService
{
    Task<TestDetailsDto> CreateAsync(TestCreateDto dto);
    Task<TestDetailsDto> UpdateAsync(Guid id, TestUpdateDto dto);
    Task<bool> DeleteAsync(Guid id);
    Task<TestDetailsDto> GetByIdAsync(Guid id);
    Task<IEnumerable<TestListDto>> GetAllAsync();
    Task<PagedResultDto<TestListDto>> GetAllPagedAsync(int page, int pageSize, string? search, Guid? subjectId, bool onlyPublished, string? difficulty = null, Guid? topicId = null);

    // O'quvchi testni boshlaganda hamma narsani (IsCorrect'larsiz) yuklab beradigan metod
    Task<StudentTestDto> GetForStudentAsync(Guid id);

    // Testni faollashtirish yoki vaqtincha yopib qo'yish (Publish/Unpublish) uchun
    Task<bool> TogglePublishStatusAsync(Guid id);
}