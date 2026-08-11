using TestPlatform.Service.DTOs.Subjects;
using TestPlatform.Service.DTOs.Pagination;

namespace TestPlatform.Service.Interfaces;

public interface ISubjectService
{
    Task<SubjectResultDto> CreateAsync(SubjectCreateDto dto);
    Task<PagedResultDto<SubjectResultDto>> GetAllAsync(int page, int pageSize, string? search);
    Task<SubjectResultDto?> GetByIdAsync(Guid id);
    Task<SubjectResultDto> UpdateAsync(Guid id, SubjectCreateDto dto);
    Task<bool> DeleteAsync(Guid id);
}