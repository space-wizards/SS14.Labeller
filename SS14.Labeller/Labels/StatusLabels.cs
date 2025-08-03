namespace SS14.Labeller.Labels;

public static class StatusLabels
{
    const string Prefix = "S: ";

    public const string Untriaged = Prefix + "Untriaged";
    public const string RequireReview = Prefix + "Needs Review"; // no idea why its called this
    public const string AwaitingChanges = Prefix + "Awaiting Changes";
    public const string Approved = Prefix + "Approved";
}