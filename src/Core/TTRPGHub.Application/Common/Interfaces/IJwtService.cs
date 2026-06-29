using TTRPGHub.Entities;

namespace TTRPGHub.Common.Interfaces;

public interface IJwtService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
}
