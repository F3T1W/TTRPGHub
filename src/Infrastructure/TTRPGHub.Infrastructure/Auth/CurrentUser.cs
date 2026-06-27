using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using TTRPGHub.Application.Common.Interfaces;
using TTRPGHub.Domain.Entities;

namespace TTRPGHub.Infrastructure.Auth;

internal sealed class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    private ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;

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

    public string Username =>
        User?.FindFirstValue(JwtRegisteredClaimNames.UniqueName)
        ?? User?.FindFirstValue(ClaimTypes.Name)
        ?? string.Empty;

    public bool IsAuthenticated =>
        User?.Identity?.IsAuthenticated is true;
}
