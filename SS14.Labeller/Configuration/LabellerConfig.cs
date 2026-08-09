namespace SS14.Labeller.Configuration;

public class LabellerConfig
{
    public const string Name = "Labeller";

    public string ForwardIssuesToRepository { get; set; } = "space-station-14";

    public string[] LabelIssuesOnRepositories { get; set; } = ["space-station-14", "RobustToolbox"];
}