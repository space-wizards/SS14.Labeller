using SS14.Labeller.Models;

namespace SS14.Labeller.Handlers;

public sealed class GitHubWebhookHandlerService(IReadOnlyDictionary<Type, RequestHandlerBase> handlers)
{
    public async Task Handle(EventBase @event, CancellationToken ct)
    {
        if (handlers.TryGetValue(@event.GetType(), out var handler))
            await handler.Handle(@event, ct);
    }
}