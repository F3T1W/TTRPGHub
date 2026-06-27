using TTRPGHub.Domain.Entities;

namespace TTRPGHub.Application.Common.Interfaces;

public interface IJwtService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
}
