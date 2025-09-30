namespace SS14.Labeller.Initialization;

/// <summary>
/// Background service for application initialization.
/// Starts initialization for classes marked with <see cref="IOnApplicationStartInitializable"/> interface.
/// </summary>
public class ApplicationServicesInitializingBackgroundService : BackgroundService
{
    private readonly IReadOnlyCollection<IOnApplicationStartInitializable> _services;
    private readonly ILogger<ApplicationServicesInitializingBackgroundService> _logger;

    /// <summary> c-tor. </summary>
    public ApplicationServicesInitializingBackgroundService(
        IEnumerable<IOnApplicationStartInitializable> initializableServiceCollection,
        ILogger<ApplicationServicesInitializingBackgroundService> logger
    )
    {
        _services = (
            initializableServiceCollection ?? throw new ArgumentNullException(nameof(initializableServiceCollection))
        ).ToArray();
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await ProcessInitializables(stoppingToken);

        _logger.LogInformation("Application initialization is finished.");
    }

    private async Task ProcessInitializables(CancellationToken ct)
    {
        if (_services.Count == 0)
        {
            const string noInitMessage = "There is no initializable classes found, app initialization is not required. ";
            _logger.LogInformation(noInitMessage);
            return;
        }

        _logger.LogInformation(
            "Starting application initialization. Found {serviceCount} initializable services"
            + $"(that implement {nameof(IOnApplicationStartInitializable)}).",
            _services.Count
        );
        foreach (var initializable in _services)
        {
            ct.ThrowIfCancellationRequested();

            var args = $"{initializable.GetType().FullName}.{nameof(IOnApplicationStartInitializable.Initialize)}";
            _logger.LogDebug("Starting {called}.", args);
            await initializable.Initialize(ct);
            _logger.LogTrace("Finished {called}.", args);
        }
    }
}