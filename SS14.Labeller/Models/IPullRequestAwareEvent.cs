namespace SS14.Labeller.Models;

public interface IPullRequestAwareEvent
{
    GithubRepo Repository { get; }
    PullRequest PullRequest { get; }
}