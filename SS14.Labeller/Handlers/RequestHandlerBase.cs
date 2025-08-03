using SS14.Labeller.Models;
using System.Text;
using System.Text.Json;

namespace SS14.Labeller.Handlers;

public abstract class RequestHandlerBase
{
    /// <summary>
    /// Github-declared type of event this handler should process.
    /// </summary>
    public abstract string EventType { get; }

    /// <summary> Process event. </summary>
    /// <param name="bodyBytes">bytes of request body.</param>
    /// <param name="ct">Operation cancellation token.</param>
    public abstract Task Handle(byte[] bodyBytes, CancellationToken ct);
}

public abstract class RequestHandlerBase<T> : RequestHandlerBase where T : EventBase
{
    /// <inheritdoc />
    public override Task Handle(byte[] bodyBytes, CancellationToken ct)
    {
        var bodyString = Encoding.UTF8.GetString(bodyBytes);

        var deserialized = (T?)JsonSerializer.Deserialize(bodyString, typeof(T), SourceGenerationContext.DeserializationContext);
        if (deserialized == null)
        {
            throw new InvalidOperationException($"Failed to parse request into {typeof(T).Name} according with event type {EventType}.");
        }

        return HandleInternal(deserialized, ct);

    }

    /// <summary> Process provided event. </summary>
    protected abstract Task HandleInternal(T request, CancellationToken ct);
}