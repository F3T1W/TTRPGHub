using MediatR;
using Microsoft.Extensions.Logging;
using TTRPGHub.Common.Interfaces;

namespace TTRPGHub.Common.Behaviors;

public sealed partial class CachingBehaviour<TRequest, TResponse>(
    ICacheService cache,
    ILogger<CachingBehaviour<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        if (request is not ICacheableQuery cacheableQuery)
            return await next();

        var cached = await cache.GetAsync<TResponse>(cacheableQuery.CacheKey, ct);
        if (cached is not null)
        {
            LogCacheHit(logger, cacheableQuery.CacheKey);
            return cached;
        }

        var response = await next();

        await cache.SetAsync(cacheableQuery.CacheKey, response, cacheableQuery.Expiration, ct);
        return response;
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Cache hit: {Key}")]
    private static partial void LogCacheHit(ILogger logger, string key);
}
