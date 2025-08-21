using System.Net;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using NUnit.Framework;
using SS14.Labeller.Labelling.Labels;
using SS14.Labeller.Models;

namespace SS14.Labeller.Tests.IntegrationTests;

public partial class HandlersTests
{

    [Test]
    public async Task PullRequestReview_ReviewByNonMaintainer_DoNothing()
    {
        // Arrange
        const string fileName = "pull_request_review_approve.json";
        var requestContent = await CreateRequestContent(fileName, "pull_request_review");

        _applicationFactory.GitHubApiClient.IsMaintainer(
            "NonFildrance",
            Arg.Is<GithubRepo>(x => x.Name == "SS14.Labeller" && x.Owner.Login == "Fildrance"),
            Arg.Any<CancellationToken>()
        ).Returns(Task.FromResult(true));

        // Act
        var result = await _client.PostAsync("/webhook", requestContent);

        // Assert
        var respText = await result.Content.ReadAsStringAsync();
        Assert.That(
            result.StatusCode,
            Is.EqualTo(HttpStatusCode.NoContent),
            $"Invalid response status - {result.StatusCode}, response text: \r\n{respText}."
        );

        await _applicationFactory.GitHubApiClient
                                 .DidNotReceive()
                                 .AddLabel(
                                     Arg.Any<GithubRepo>(),
                                     Arg.Any<int>(),
                                     Arg.Any<LabelBase>(),
                                     Arg.Any<CancellationToken>()
                                 );
    }

    [Test]
    public async Task PullRequestReview_ApproveByMaintainer_SwitchStatusLabels()
    {
        // Arrange
        const string fileName = "pull_request_review_approve.json";
        var requestContent = await CreateRequestContent(fileName, "pull_request_review");

        _applicationFactory.GitHubApiClient.IsMaintainer(
            "Fildrance",
            Arg.Is<GithubRepo>(x => x.Name == "SS14.Labeller" && x.Owner.Login == "Fildrance"),
            Arg.Any<CancellationToken>()
        ).Returns(Task.FromResult(true));

        // Act
        var result = await _client.PostAsync("/webhook", requestContent);

        // Assert
        var respText = await result.Content.ReadAsStringAsync();
        Assert.That(
            result.StatusCode,
            Is.EqualTo(HttpStatusCode.NoContent),
            $"Invalid response status - {result.StatusCode}, response text: \r\n{respText}."
        );

        await _applicationFactory.GitHubApiClient
                                 .Received()
                                 .AddLabel(
                                     Arg.Is<GithubRepo>(x => x.Name == "SS14.Labeller" && x.Owner.Login == "Fildrance"),
                                     4,
                                     StatusLabel.Approved,
                                     Arg.Any<CancellationToken>()
                                 );
    }

    [Test]
    public async Task PullRequestReview_RequestChangesByMaintainer_SwitchStatusLabels()
    {
        // Arrange
        const string fileName = "pull_request_review_request_changes.json";
        var requestContent = await CreateRequestContent(fileName, "pull_request_review");

        _applicationFactory.GitHubApiClient.IsMaintainer(
            "Fildrance",
            Arg.Is<GithubRepo>(x => x.Name == "SS14.Labeller" && x.Owner.Login == "Fildrance"),
            Arg.Any<CancellationToken>()
        ).Returns(Task.FromResult(true));

        // Act
        var result = await _client.PostAsync("/webhook", requestContent);

        // Assert
        var respText = await result.Content.ReadAsStringAsync();
        Assert.That(
            result.StatusCode,
            Is.EqualTo(HttpStatusCode.NoContent),
            $"Invalid response status - {result.StatusCode}, response text: \r\n{respText}."
        );

        await _applicationFactory.GitHubApiClient
                                 .Received()
                                 .AddLabel(
                                     Arg.Is<GithubRepo>(x => x.Name == "SS14.Labeller" && x.Owner.Login == "Fildrance"),
                                     4,
                                     StageOfWorkLabel.AwaitingChanges,
                                     Arg.Any<CancellationToken>()
                                 );

        await _applicationFactory.GitHubApiClient
                                 .Received()
                                 .RemoveLabel(
                                     Arg.Is<GithubRepo>(x => x.Name == "SS14.Labeller" && x.Owner.Login == "Fildrance"),
                                     4,
                                     StageOfWorkLabel.RequireReview,
                                     Arg.Any<CancellationToken>()
                                 );
    }

    [Test]
    public async Task PullRequestReview_CommentedMergedByMaintainer_DoNothing()
    {
        // Arrange
        const string fileName = "pull_request_review_approve_merged.json";
        var requestContent = await CreateRequestContent(fileName, "pull_request_review");

        _applicationFactory.GitHubApiClient.IsMaintainer(
            "Fildrance",
            Arg.Is<GithubRepo>(x => x.Name == "SS14.Labeller" && x.Owner.Login == "Fildrance"),
            Arg.Any<CancellationToken>()
        ).Returns(Task.FromResult(true));

        // Act
        var result = await _client.PostAsync("/webhook", requestContent);

        // Assert
        var respText = await result.Content.ReadAsStringAsync();
        Assert.That(
            result.StatusCode,
            Is.EqualTo(HttpStatusCode.NoContent),
            $"Invalid response status - {result.StatusCode}, response text: \r\n{respText}."
        );

        await _applicationFactory.GitHubApiClient
                                 .DidNotReceive()
                                 .AddLabel(
                                     Arg.Any<GithubRepo>(),
                                     Arg.Any<int>(),
                                     Arg.Any<LabelBase>(),
                                     Arg.Any<CancellationToken>()
                                 );

        await _applicationFactory.GitHubApiClient
                                 .DidNotReceive()
                                 .RemoveLabel(
                                     Arg.Any<GithubRepo>(),
                                     Arg.Any<int>(),
                                     Arg.Any<LabelBase>(),
                                     Arg.Any<CancellationToken>()
                                 );
    }

    [Test]
    public async Task PullRequestReview_CommentedByMaintainer_DoNothing()
    {
        // Arrange
        const string fileName = "pull_request_review_commented.json";
        var requestContent = await CreateRequestContent(fileName, "pull_request_review");

        _applicationFactory.GitHubApiClient.IsMaintainer(
            "Fildrance",
            Arg.Is<GithubRepo>(x => x.Name == "SS14.Labeller" && x.Owner.Login == "Fildrance"),
            Arg.Any<CancellationToken>()
        ).Returns(Task.FromResult(true));

        // Act
        var result = await _client.PostAsync("/webhook", requestContent);

        // Assert
        var respText = await result.Content.ReadAsStringAsync();
        Assert.That(
            result.StatusCode,
            Is.EqualTo(HttpStatusCode.NoContent),
            $"Invalid response status - {result.StatusCode}, response text: \r\n{respText}."
        );

        await _applicationFactory.GitHubApiClient
                                 .DidNotReceive()
                                 .AddLabel(
                                     Arg.Any<GithubRepo>(),
                                     Arg.Any<int>(),
                                     Arg.Any<LabelBase>(),
                                     Arg.Any<CancellationToken>()
                                 );

        await _applicationFactory.GitHubApiClient
                                 .DidNotReceive()
                                 .RemoveLabel(
                                     Arg.Any<GithubRepo>(),
                                     Arg.Any<int>(),
                                     Arg.Any<LabelBase>(),
                                     Arg.Any<CancellationToken>()
                                 );
    }
}