using SS14.Labeller.Models;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace SS14.Labeller.Handlers;

public abstract class RequestHandlerBase
{


    public abstract string EventType { get; }

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

    protected abstract Task HandleInternal(T request, CancellationToken ct);
}