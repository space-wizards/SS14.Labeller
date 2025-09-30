using System.Collections.Concurrent;
using MessagePipe;

namespace SS14.Labeller;

public class GenericPublisher(IServiceProvider sp)
{
    private static readonly ConcurrentDictionary<Type, Func<object, CancellationToken, ValueTask>> HandlingDelegates = new();

    public ValueTask PublishByType(object message, CancellationToken ct)
    {
        var type = message.GetType();

        var handlingDelegate = HandlingDelegates.GetOrAdd(type, (t, state) =>
        {
            var methodInfo = state.GetType().GetMethod(nameof(Publish));
            var targetMethod = methodInfo?.MakeGenericMethod(t);
            return (object msg, CancellationToken tkn) =>
            {
                var result = targetMethod?.Invoke(this, [msg, tkn]);
                return result == null 
                    ? ValueTask.CompletedTask 
                    : (ValueTask)result;
            };
        }, this);

        return handlingDelegate.Invoke(message, ct);
    }

    public ValueTask Publish<T>(T message, CancellationToken ct)
    {
        var publisher = sp.GetRequiredService<IAsyncPublisher<T>>();
        return publisher.PublishAsync(message, cancellationToken: ct);
    }
}