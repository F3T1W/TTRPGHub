using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using TTRPGHub.Common.Interfaces;

namespace TTRPGHub.Caching;

internal sealed class CacheService(
    IDistributedCache cache,
    IOptions<RedisCacheOptions> redisOptions) : ICacheService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private IConnectionMultiplexer? _redis;

    private IConnectionMultiplexer GetRedis()
    {
        if (_redis is not null) return _redis;
        var config = redisOptions.Value.Configuration
            ?? redisOptions.Value.ConfigurationOptions?.ToString()
            ?? "localhost:6379";
        _redis = ConnectionMultiplexer.Connect(config);
        return _redis;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        var bytes = await cache.GetAsync(key, ct);
        return bytes is null ? default : JsonSerializer.Deserialize<T>(bytes, JsonOptions);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken ct = default)
    {
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration ?? TimeSpan.FromMinutes(10)
        };
        var bytes = JsonSerializer.SerializeToUtf8Bytes(value, JsonOptions);
        await cache.SetAsync(key, bytes, options, ct);
    }

    public Task RemoveAsync(string key, CancellationToken ct = default) =>
        cache.RemoveAsync(key, ct);
}
