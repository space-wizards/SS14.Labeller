using MessagePipe;
using SS14.Labeller.Models;

namespace SS14.Labeller.Initialization;

public class MessagePipeSubscriptionManager(IServiceProvider sp, IHostApplicationLifetime lifetime) : IOnApplicationStartInitializable
{
    /// <inheritdoc />
    public Task Initialize(CancellationToken ct)

    {
        List<IDisposable> subscriptions =
        [
            Sub<IssuesEvent>(sp),
            Sub<PullRequestEvent>(sp),
            Sub<PullRequestReviewEvent>(sp),
        ];

        lifetime.ApplicationStopping.Register(x =>
        {
            if(x is not List<IDisposable> disposables)
                return;

            foreach (var subscription in disposables)
            {
                subscription.Dispose();
            }
        }, subscriptions);

        Initialized = true;
        return Task.CompletedTask;
    }

    private static IDisposable Sub<TEvent>(IServiceProvider sp)
    {
        var handler = sp.GetRequiredService<IAsyncMessageHandler<TEvent>>();
        var sub = sp.GetRequiredService<IAsyncSubscriber<TEvent>>();
        return sub.Subscribe(handler);
    }

    /// <inheritdoc />
    public bool Initialized { get; private set; }
}