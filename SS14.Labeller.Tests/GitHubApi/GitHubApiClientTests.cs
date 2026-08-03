using System;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using SS14.Labeller.Configuration;
using SS14.Labeller.GitHubApi;
using SS14.Labeller.Labelling.Labels;
using SS14.Labeller.Models;
using SS14.Labeller.Tests.GitHubApi.Mocks;

namespace SS14.Labeller.Tests.GitHubApi;

[Category("Unit"), ExcludeFromCodeCoverage]
public class GitHubApiClientTests
{
    private GitHubConfig _config = default!;
    private MockInnerHandler _innerHandler = default!;

    [SetUp]
    public void Setup()
    {
        _config = new GitHubConfig
        {
            Owner = "space-wizards",
            Repo = "space-station-14"
        };
        _innerHandler = new MockInnerHandler();
    }

    [Test]
    public async Task AddLabel_MatchingRepo_PerformsRequest()
    {
        // Arrange
        var client = new GitHubApiClient(new HttpClient(_innerHandler), Options.Create(_config));

        // Act
        await client.AddLabel("space-wizards", "space-station-14", 1, StatusLabel.Approved, CancellationToken.None);

        // Assert
        Assert.That(_innerHandler.Requests.Count, Is.EqualTo(1));
    }

    [Test]
    public void AddLabel_MismatchedRepo_Throws()
    {
        // Arrange
        var client = new GitHubApiClient(new HttpClient(_innerHandler), Options.Create(_config));

        // Act & Assert
        var ex = Assert.ThrowsAsync<InvalidOperationException>(() =>
            client.AddLabel("other-user", "other-repo", 1, StatusLabel.Approved, CancellationToken.None));

        Assert.That(ex!.Message, Does.Contain("does not match the configured repository"));
        Assert.That(_innerHandler.Requests, Is.Empty);
    }

    [Test]
    public void AddLabel_MismatchedOwner_Throws()
    {
        // Arrange
        var client = new GitHubApiClient(new HttpClient(_innerHandler), Options.Create(_config));

        // Act & Assert
        Assert.ThrowsAsync<InvalidOperationException>(() =>
            client.AddLabel("other-user", "space-station-14", 1, StatusLabel.Approved, CancellationToken.None));

        Assert.That(_innerHandler.Requests, Is.Empty);
    }

    [Test]
    public void AddLabel_MismatchedRepoName_Throws()
    {
        // Arrange
        var client = new GitHubApiClient(new HttpClient(_innerHandler), Options.Create(_config));

        // Act & Assert
        Assert.ThrowsAsync<InvalidOperationException>(() =>
            client.AddLabel("space-wizards", "other-repo", 1, StatusLabel.Approved, CancellationToken.None));

        Assert.That(_innerHandler.Requests, Is.Empty);
    }

    [Test]
    public void GetComments_MismatchedRepo_Throws()
    {
        // Arrange
        var client = new GitHubApiClient(new HttpClient(_innerHandler), Options.Create(_config));
        var repo = new GithubRepo
        {
            Name = "other-repo",
            Owner = new User { Login = "other-user" }
        };

        // Act & Assert
        Assert.ThrowsAsync<InvalidOperationException>(() =>
            client.GetComments(repo, 1, CancellationToken.None));

        Assert.That(_innerHandler.Requests, Is.Empty);
    }

}
