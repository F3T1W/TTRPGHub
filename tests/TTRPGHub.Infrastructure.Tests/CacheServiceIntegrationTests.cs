using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Options;
using Testcontainers.Redis;
using TTRPGHub.Caching;

namespace TTRPGHub.Infrastructure.Tests;

// Exercises CacheService against a real Redis instance (via Testcontainers) rather than mocking
// IDistributedCache — the class under test is a thin wrapper whose only real risk is serialization
// round-tripping and TTL/removal semantics actually reaching a real Redis server.
public sealed class CacheServiceIntegrationTests : IAsyncLifetime
{
    private readonly RedisContainer _redis = new RedisBuilder().WithImage("redis:7-alpine").Build();
    private RedisCache _distributedCache = null!;
    private CacheService _sut = null!;

    public async Task InitializeAsync()
    {
        await _redis.StartAsync();
        var options = new RedisCacheOptions { Configuration = _redis.GetConnectionString() };
        _distributedCache = new RedisCache(Options.Create(options));
        _sut = new CacheService(_distributedCache, Options.Create(options));
    }

    public async Task DisposeAsync()
    {
        _distributedCache.Dispose();
        await _redis.DisposeAsync();
    }

    private sealed record Sample(string Name, int Level);

    [Fact]
    public async Task GetAsync_MissingKey_ReturnsDefault()
    {
        var result = await _sut.GetAsync<Sample>("missing-key");

        Assert.Null(result);
    }

    [Fact]
    public async Task SetAsync_ThenGetAsync_RoundTripsValue()
    {
        await _sut.SetAsync("character:1", new Sample("Grog", 5));

        var result = await _sut.GetAsync<Sample>("character:1");

        Assert.NotNull(result);
        Assert.Equal("Grog", result!.Name);
        Assert.Equal(5, result.Level);
    }

    [Fact]
    public async Task RemoveAsync_DeletesKeyFromRedis()
    {
        await _sut.SetAsync("to-remove", new Sample("Temp", 1));

        await _sut.RemoveAsync("to-remove");

        Assert.Null(await _sut.GetAsync<Sample>("to-remove"));
    }

    [Fact]
    public async Task SetAsync_ShortExpiration_ValueExpiresFromRedis()
    {
        await _sut.SetAsync("short-lived", new Sample("Ephemeral", 1), TimeSpan.FromMilliseconds(200));

        await Task.Delay(TimeSpan.FromSeconds(1));

        Assert.Null(await _sut.GetAsync<Sample>("short-lived"));
    }

    [Fact]
    public async Task SetAsync_OverwritesPreviousValueForSameKey()
    {
        await _sut.SetAsync("character:2", new Sample("First", 1));
        await _sut.SetAsync("character:2", new Sample("Second", 2));

        var result = await _sut.GetAsync<Sample>("character:2");

        Assert.Equal("Second", result!.Name);
    }
}
