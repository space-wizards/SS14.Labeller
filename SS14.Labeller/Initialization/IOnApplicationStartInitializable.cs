namespace SS14.Labeller.Initialization;

/// <summary>
/// Initializable object in system. Can be singleton service, job or something else.
/// <see cref="Initialize"/> will be called by background service, but <see cref="Initialized"/>
/// should be set to True manually during this call.
/// </summary>
public interface IOnApplicationStartInitializable
{
    /// <summary>
    /// Initializes component of system.
    /// </summary>
    public Task Initialize(CancellationToken ct);

    /// <summary>
    /// Is component initialized already?
    /// </summary>
    /// <remarks>
    /// This is used in health-check and will be regularly requested using background service.
    /// </remarks>
    public bool Initialized { get; }
}