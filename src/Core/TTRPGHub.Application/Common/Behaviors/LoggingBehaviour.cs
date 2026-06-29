using MediatR;
using Microsoft.Extensions.Logging;

namespace TTRPGHub.Common.Behaviors;

public sealed partial class LoggingBehaviour<TRequest, TResponse>(
    ILogger<LoggingBehaviour<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        var name = typeof(TRequest).Name;
        LogHandling(logger, name);
        var response = await next();
        LogHandled(logger, name);
        return response;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Handling {Request}")]
    private static partial void LogHandling(ILogger logger, string request);

    [LoggerMessage(Level = LogLevel.Information, Message = "Handled {Request}")]
    private static partial void LogHandled(ILogger logger, string request);
}
