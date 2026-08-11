using TestPlatform.Domain.Entities;

namespace TestPlatform.Service.Interfaces;

public interface IJwtService
{
    string GenerateToken(User user);
}
