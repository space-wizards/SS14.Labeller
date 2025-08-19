using SS14.Labeller.Labelling.Labels;
using SS14.Labeller.Models;

namespace SS14.Labeller.Labelling;

public interface ILabelManager
{
    Task EnsureLabeled(IPullRequestAwareEvent @event, LabelBase requestedLabel, CancellationToken ct);

    Task EnsureNotLabeled(IPullRequestAwareEvent @event, LabelBase requestedLabel, CancellationToken ct);
}