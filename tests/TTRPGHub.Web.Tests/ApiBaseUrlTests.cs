using Microsoft.Extensions.Configuration;
using NSubstitute;
using TTRPGHub.Services;

namespace TTRPGHub.Web.Tests;

public class ApiBaseUrlTests
{
    private static IConfiguration ConfigWith(string? apiBaseUrl)
    {
        var config = Substitute.For<IConfiguration>();
        config["ApiBaseUrl"].Returns(apiBaseUrl);
        return config;
    }

    [Fact]
    public void Resolve_ConfiguredNonLocalhostUrl_ReturnsItAsIs()
    {
        var config = ConfigWith("https://api.example.com/");

        var result = ApiBaseUrl.Resolve(config, "https://app.example.com/");

        Assert.Equal("https://api.example.com", result);
    }

    [Theory]
    [InlineData("http://localhost:5014")]
    [InlineData("http://127.0.0.1:5014")]
    public void Resolve_ConfiguredLocalhostUrl_FallsBackToWasmHost(string configured)
    {
        var config = ConfigWith(configured);

        var result = ApiBaseUrl.Resolve(config, "http://192.168.1.50:5141/");

        Assert.Equal("http://192.168.1.50:5014", result);
    }

    [Fact]
    public void Resolve_NoConfiguredUrl_UsesWasmHostWithDefaultPort()
    {
        var config = ConfigWith(null);

        var result = ApiBaseUrl.Resolve(config, "http://localhost:5141/");

        Assert.Equal("http://localhost:5014", result);
    }

    [Fact]
    public void Resolve_BlankConfiguredUrl_UsesWasmHost()
    {
        var config = ConfigWith("   ");

        var result = ApiBaseUrl.Resolve(config, "http://localhost:5141/");

        Assert.Equal("http://localhost:5014", result);
    }

    [Fact]
    public void Resolve_MalformedConfiguredUrl_UsesWasmHost()
    {
        var config = ConfigWith("not-a-valid-uri");

        var result = ApiBaseUrl.Resolve(config, "http://localhost:5141/");

        Assert.Equal("http://localhost:5014", result);
    }
}
