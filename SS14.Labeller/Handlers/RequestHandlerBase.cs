using SS14.Labeller.Models;

namespace SS14.Labeller.Handlers;

public abstract class RequestHandlerBase
{
    /// <summary>
    /// Github-declared type of event this handler should process.
    /// </summary>
    public abstract Type CanHandleType { get; }

    /// <summary> Process event. </summary>
    /// <param name="eventFromRequest">bytes of request body.</param>
    /// <param name="ct">Operation cancellation token.</param>
    public abstract Task Handle(EventBase eventFromRequest, CancellationToken ct);
}

public abstract class RequestHandlerBase<T> : RequestHandlerBase where T : EventBase
{
    /// <inheritdoc />
    public override Task Handle(EventBase eventFromRequest, CancellationToken ct)
    {
        return HandleInternal((T)eventFromRequest, ct);
    }

    /// <summary> Process provided event. </summary>
    protected abstract Task HandleInternal(T request, CancellationToken ct);

    /// <inheritdoc />
    public override Type CanHandleType => typeof(T);
}