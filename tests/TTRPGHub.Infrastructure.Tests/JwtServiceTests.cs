using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using TTRPGHub.Auth;
using TTRPGHub.Entities;
using TTRPGHub.ValueObjects;

namespace TTRPGHub.Infrastructure.Tests;

public class JwtServiceTests
{
    private static IConfiguration BuildConfiguration(Dictionary<string, string?>? overrides = null)
    {
        var values = new Dictionary<string, string?>
        {
            ["Jwt:Secret"] = "super-secret-signing-key-that-is-long-enough-1234567890",
            ["Jwt:Issuer"] = "TTRPGHub",
            ["Jwt:Audience"] = "TTRPGHub.Web",
            ["Jwt:ExpiryMinutes"] = "480",
        };
        if (overrides is not null)
            foreach (var (key, value) in overrides)
                values[key] = value;

        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }

    private static User CreateUser() => User.Create("grog", ValueObjects.Email.Create("grog@test.com").Value!, "hash");

    [Fact]
    public void GenerateAccessToken_MissingSecret_Throws()
    {
        var configuration = BuildConfiguration(new() { ["Jwt:Secret"] = null });
        var service = new JwtService(configuration);

        Assert.Throws<InvalidOperationException>(() => service.GenerateAccessToken(CreateUser()));
    }

    [Fact]
    public void GenerateAccessToken_ProducesTokenWithExpectedClaims()
    {
        var service = new JwtService(BuildConfiguration());
        var user = CreateUser();
        user.SetRole(UserRole.Moderator);

        var token = service.GenerateAccessToken(user);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.Equal(user.Id.Value.ToString(), jwt.Claims.Single(c => c.Type == JwtRegisteredClaimNames.Sub).Value);
        Assert.Equal("grog", jwt.Claims.Single(c => c.Type == JwtRegisteredClaimNames.UniqueName).Value);
        Assert.Equal("grog@test.com", jwt.Claims.Single(c => c.Type == JwtRegisteredClaimNames.Email).Value);
        Assert.Equal("Moderator", jwt.Claims.Single(c => c.Type == ClaimTypes.Role).Value);
    }

    [Fact]
    public void GenerateAccessToken_UsesConfiguredIssuerAndAudience()
    {
        var service = new JwtService(BuildConfiguration());
        var token = service.GenerateAccessToken(CreateUser());
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.Equal("TTRPGHub", jwt.Issuer);
        Assert.Equal("TTRPGHub.Web", jwt.Audiences.Single());
    }

    [Fact]
    public void GenerateAccessToken_UsesConfiguredExpiryMinutes()
    {
        var service = new JwtService(BuildConfiguration(new() { ["Jwt:ExpiryMinutes"] = "15" }));
        var before = DateTime.UtcNow;

        var token = service.GenerateAccessToken(CreateUser());
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.True(jwt.ValidTo <= before.AddMinutes(15).AddSeconds(5));
        Assert.True(jwt.ValidTo >= before.AddMinutes(15).AddSeconds(-5));
    }

    [Fact]
    public void GenerateAccessToken_InvalidExpiryMinutesConfig_FallsBackToDefault()
    {
        var service = new JwtService(BuildConfiguration(new() { ["Jwt:ExpiryMinutes"] = "not-a-number" }));
        var before = DateTime.UtcNow;

        var token = service.GenerateAccessToken(CreateUser());
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.True(jwt.ValidTo >= before.AddMinutes(480).AddSeconds(-5));
    }

    [Fact]
    public void GenerateAccessToken_TwoCallsForSameUser_ProduceDifferentJtiClaims()
    {
        var service = new JwtService(BuildConfiguration());
        var user = CreateUser();

        var jwt1 = new JwtSecurityTokenHandler().ReadJwtToken(service.GenerateAccessToken(user));
        var jwt2 = new JwtSecurityTokenHandler().ReadJwtToken(service.GenerateAccessToken(user));

        var jti1 = jwt1.Claims.Single(c => c.Type == JwtRegisteredClaimNames.Jti).Value;
        var jti2 = jwt2.Claims.Single(c => c.Type == JwtRegisteredClaimNames.Jti).Value;
        Assert.NotEqual(jti1, jti2);
    }

    [Fact]
    public void GenerateRefreshToken_ProducesUniqueValuesEachCall()
    {
        var service = new JwtService(BuildConfiguration());

        var token1 = service.GenerateRefreshToken();
        var token2 = service.GenerateRefreshToken();

        Assert.NotEqual(token1, token2);
    }

    [Fact]
    public void GenerateRefreshToken_ProducesA64ByteBase64Value()
    {
        var service = new JwtService(BuildConfiguration());

        var token = service.GenerateRefreshToken();

        Assert.Equal(64, Convert.FromBase64String(token).Length);
    }
}
