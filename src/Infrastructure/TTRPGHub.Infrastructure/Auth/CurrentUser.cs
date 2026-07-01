using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;

namespace TTRPGHub.Auth;

internal sealed class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    private ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated =>
        User?.Identity?.IsAuthenticated is true;

    public UserId Id
    {
        get
        {
            var sub = User?.FindFirstValue(JwtRegisteredClaimNames.Sub)
                ?? User?.FindFirstValue(ClaimTypes.NameIdentifier);
            return sub is not null && Guid.TryParse(sub, out var guid)
                ? new UserId(guid)
                : UserId.Empty;
        }
    }

    public UserRole Role
    {
        get
        {
            var role = User?.FindFirstValue(ClaimTypes.Role);
            return role is not null && Enum.TryParse<UserRole>(role, out var parsed)
                ? parsed
                : UserRole.Player;
        }
    }
}
