using System.Text;
using System.Text.Json;

namespace SS14.Labeller.Handlers;

public abstract class RequestHandlerBase
{
    protected readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public abstract string EventType { get; }

    public abstract Task Handle(byte[] bodyBytes, CancellationToken ct);
}

public abstract class RequestHandlerBase<T> : RequestHandlerBase
{
    /// <inheritdoc />
    public override Task Handle(byte[] bodyBytes, CancellationToken ct)
    {
        var bodyString = Encoding.UTF8.GetString(bodyBytes);

        var deserialized = JsonSerializer.Deserialize<T>(bodyString, JsonSerializerOptions);
        if (deserialized == null)
        {
            throw new InvalidOperationException($"Failed to parse request into {typeof(T).Name} according with event type {EventType}.");
        }

        return HandleInternal(deserialized, ct);

    }

    protected abstract Task HandleInternal(T request, CancellationToken ct);
}