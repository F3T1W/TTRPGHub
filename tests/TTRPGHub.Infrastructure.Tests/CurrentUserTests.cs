using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using TTRPGHub.Auth;
using TTRPGHub.Entities;

namespace TTRPGHub.Infrastructure.Tests;

public class CurrentUserTests
{
    private static IHttpContextAccessor CreateAccessor(ClaimsPrincipal? principal)
    {
        var context = new DefaultHttpContext();
        if (principal is not null)
            context.User = principal;
        return new HttpContextAccessor { HttpContext = principal is null ? null : context };
    }

    private static ClaimsPrincipal AuthenticatedPrincipal(params Claim[] claims) =>
        new(new ClaimsIdentity(claims, authenticationType: "TestAuth"));

    [Fact]
    public void IsAuthenticated_NoHttpContext_IsFalse()
    {
        var currentUser = new CurrentUser(CreateAccessor(null));

        Assert.False(currentUser.IsAuthenticated);
    }

    [Fact]
    public void IsAuthenticated_UnauthenticatedPrincipal_IsFalse()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity());
        var currentUser = new CurrentUser(CreateAccessor(principal));

        Assert.False(currentUser.IsAuthenticated);
    }

    [Fact]
    public void IsAuthenticated_AuthenticatedPrincipal_IsTrue()
    {
        var principal = AuthenticatedPrincipal();
        var currentUser = new CurrentUser(CreateAccessor(principal));

        Assert.True(currentUser.IsAuthenticated);
    }

    [Fact]
    public void Id_NoSubOrNameIdentifierClaim_ReturnsEmpty()
    {
        var principal = AuthenticatedPrincipal();
        var currentUser = new CurrentUser(CreateAccessor(principal));

        Assert.Equal(UserId.Empty, currentUser.Id);
    }

    [Fact]
    public void Id_ValidSubClaim_ParsesGuid()
    {
        var userId = Guid.NewGuid();
        var principal = AuthenticatedPrincipal(new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()));
        var currentUser = new CurrentUser(CreateAccessor(principal));

        Assert.Equal(userId, currentUser.Id.Value);
    }

    [Fact]
    public void Id_FallsBackToNameIdentifierClaimWhenSubMissing()
    {
        var userId = Guid.NewGuid();
        var principal = AuthenticatedPrincipal(new Claim(ClaimTypes.NameIdentifier, userId.ToString()));
        var currentUser = new CurrentUser(CreateAccessor(principal));

        Assert.Equal(userId, currentUser.Id.Value);
    }

    [Fact]
    public void Id_UnparsableSubClaim_ReturnsEmpty()
    {
        var principal = AuthenticatedPrincipal(new Claim(JwtRegisteredClaimNames.Sub, "not-a-guid"));
        var currentUser = new CurrentUser(CreateAccessor(principal));

        Assert.Equal(UserId.Empty, currentUser.Id);
    }

    [Fact]
    public void Role_NoRoleClaim_DefaultsToPlayer()
    {
        var principal = AuthenticatedPrincipal();
        var currentUser = new CurrentUser(CreateAccessor(principal));

        Assert.Equal(UserRole.Player, currentUser.Role);
    }

    [Fact]
    public void Role_ValidRoleClaim_Parses()
    {
        var principal = AuthenticatedPrincipal(new Claim(ClaimTypes.Role, "Admin"));
        var currentUser = new CurrentUser(CreateAccessor(principal));

        Assert.Equal(UserRole.Admin, currentUser.Role);
    }

    [Fact]
    public void Role_UnrecognizedRoleClaim_DefaultsToPlayer()
    {
        var principal = AuthenticatedPrincipal(new Claim(ClaimTypes.Role, "SuperAdmin"));
        var currentUser = new CurrentUser(CreateAccessor(principal));

        Assert.Equal(UserRole.Player, currentUser.Role);
    }
}
