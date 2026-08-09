namespace SS14.Labeller.Models;

public class IssuesEvent : EventBase
{
    public const string EventTypeName = "issues";

    public required Issue Issue { get; init; }

    public Label[] Labels { get; init; } = [];
}

public sealed record Issue
{
    public int Number { get; set; }

    public Label[] Labels { get; init; } = null!;
    public required string Body { get; init; }
    public required string Title { get; init; }
}
