namespace SS14.Labeller.Models;

public abstract class EventBase
{
    public required string Action { get; init; }

    public required GithubRepo Repository { get; init; }
}

public class GithubRepo
{
    public required User Owner { get; init; }

    public required string Name { get; set; }
}

public class User
{
    public required string Login { get; set; }
}

public class Label
{
    public string? Name { get; set; }
}