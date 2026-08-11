using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TestPlatform.Service.DTOs.Topics;

namespace TestPlatform.Service.Interfaces;

public interface ITopicService
{
    Task<TopicDetailsDto> CreateAsync(TopicCreateDto dto);
    Task<TopicDetailsDto> UpdateAsync(Guid id, TopicUpdateDto dto);
    Task<bool> DeleteAsync(Guid id);
    Task<TopicDetailsDto?> GetByIdAsync(Guid id);
    Task<IEnumerable<TopicDetailsDto>> GetAllAsync();
}
