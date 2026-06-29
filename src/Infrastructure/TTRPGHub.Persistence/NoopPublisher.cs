using MediatR;

namespace TTRPGHub;

internal sealed class NoopPublisher : IPublisher
{
    public Task Publish(object notification, CancellationToken ct = default) => Task.CompletedTask;
    public Task Publish<TNotification>(TNotification notification, CancellationToken ct = default)
        where TNotification : INotification => Task.CompletedTask;
}
