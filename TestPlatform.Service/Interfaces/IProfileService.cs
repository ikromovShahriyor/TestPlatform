using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TestPlatform.Service.DTOs.Profile;

namespace TestPlatform.Service.Interfaces;

public interface IProfileService
{
    Task<UserProfileDto?> GetProfileAsync(Guid userId);
    Task<UserProfileDto> UpdateProfileAsync(Guid userId, UserProfileUpdateDto dto);
    Task<IEnumerable<UserAttemptHistoryDto>> GetAttemptsAsync(Guid userId);
    Task<UserAttemptHistoryDto?> GetAttemptByIdAsync(Guid userId, Guid attemptId);
}
