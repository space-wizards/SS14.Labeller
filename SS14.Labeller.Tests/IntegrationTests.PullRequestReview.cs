using NSubstitute;
using NUnit.Framework;
using SS14.Labeller.Labels;
using SS14.Labeller.Models;
using System.Net;
using System.Threading.Tasks;
using System.Threading;

namespace SS14.Labeller.Tests;

public partial class IntegrationTests
{

    [Test]
    public async Task PullRequestReview_ReviewByNonMaintainer_DoNothing()
    {
        // Arrange
        const string fileName = "pull_request_review_approve.json";
        var requestContent = await CreateRequestContent(fileName, "pull_request_review");

        _applicationFactory.GitHubApiClient.GetPermission(
            Arg.Is<Repository>(x => x.Name == "SS14.Labeller" && x.Owner.Login == "Fildrance"),
            "NonFildrance",
            Arg.Any<CancellationToken>()
        ).Returns(Task.FromResult("user"));

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
                                     Arg.Any<Repository>(),
                                     Arg.Any<int>(),
                                     Arg.Any<string>(),
                                     Arg.Any<CancellationToken>()
                                 );
    }

    [Test]
    public async Task PullRequestReview_ApproveByMaintainer_SwitchStatusLabels()
    {
        // Arrange
        const string fileName = "pull_request_review_approve.json";
        var requestContent = await CreateRequestContent(fileName, "pull_request_review");

        _applicationFactory.GitHubApiClient.GetPermission(
            Arg.Is<Repository>(x => x.Name == "SS14.Labeller" && x.Owner.Login == "Fildrance"),
            "Fildrance",
            Arg.Any<CancellationToken>()
        ).Returns(Task.FromResult("write"));

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
                                     Arg.Is<Repository>(x => x.Name == "SS14.Labeller" && x.Owner.Login == "Fildrance"),
                                     4,
                                     StatusLabels.Approved,
                                     Arg.Any<CancellationToken>()
                                 );

        await _applicationFactory.GitHubApiClient
                                 .Received()
                                 .RemoveLabel(
                                     Arg.Is<Repository>(x => x.Name == "SS14.Labeller" && x.Owner.Login == "Fildrance"),
                                     4,
                                     StatusLabels.RequireReview,
                                     Arg.Any<CancellationToken>()
                                 );
    }

    [Test]
    public async Task PullRequestReview_RequestChangesByMaintainer_SwitchStatusLabels()
    {
        // Arrange
        const string fileName = "pull_request_review_request_changes.json";
        var requestContent = await CreateRequestContent(fileName, "pull_request_review");

        _applicationFactory.GitHubApiClient.GetPermission(
            Arg.Is<Repository>(x => x.Name == "SS14.Labeller" && x.Owner.Login == "Fildrance"),
            "Fildrance",
            Arg.Any<CancellationToken>()
        ).Returns(Task.FromResult("write"));

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
                                     Arg.Is<Repository>(x => x.Name == "SS14.Labeller" && x.Owner.Login == "Fildrance"),
                                     4,
                                     StatusLabels.AwaitingChanges,
                                     Arg.Any<CancellationToken>()
                                 );

        await _applicationFactory.GitHubApiClient
                                 .Received()
                                 .RemoveLabel(
                                     Arg.Is<Repository>(x => x.Name == "SS14.Labeller" && x.Owner.Login == "Fildrance"),
                                     4,
                                     StatusLabels.RequireReview,
                                     Arg.Any<CancellationToken>()
                                 );
    }

    [Test]
    public async Task PullRequestReview_CommentedMergedByMaintainer_DoNothing()
    {
        // Arrange
        const string fileName = "pull_request_review_approve_merged.json";
        var requestContent = await CreateRequestContent(fileName, "pull_request_review");

        _applicationFactory.GitHubApiClient.GetPermission(
            Arg.Is<Repository>(x => x.Name == "SS14.Labeller" && x.Owner.Login == "Fildrance"),
            "Fildrance",
            Arg.Any<CancellationToken>()
        ).Returns(Task.FromResult("write"));

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
                                     Arg.Any<Repository>(),
                                     Arg.Any<int>(),
                                     Arg.Any<string>(),
                                     Arg.Any<CancellationToken>()
                                 );

        await _applicationFactory.GitHubApiClient
                                 .DidNotReceive()
                                 .RemoveLabel(
                                     Arg.Any<Repository>(),
                                     Arg.Any<int>(),
                                     Arg.Any<string>(),
                                     Arg.Any<CancellationToken>()
                                 );
    }

    [Test]
    public async Task PullRequestReview_CommentedByMaintainer_DoNothing()
    {
        // Arrange
        const string fileName = "pull_request_review_commented.json";
        var requestContent = await CreateRequestContent(fileName, "pull_request_review");

        _applicationFactory.GitHubApiClient.GetPermission(
            Arg.Is<Repository>(x => x.Name == "SS14.Labeller" && x.Owner.Login == "Fildrance"),
            "Fildrance",
            Arg.Any<CancellationToken>()
        ).Returns(Task.FromResult("write"));

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
                                     Arg.Any<Repository>(),
                                     Arg.Any<int>(),
                                     Arg.Any<string>(),
                                     Arg.Any<CancellationToken>()
                                 );

        await _applicationFactory.GitHubApiClient
                                 .DidNotReceive()
                                 .RemoveLabel(
                                     Arg.Any<Repository>(),
                                     Arg.Any<int>(),
                                     Arg.Any<string>(),
                                     Arg.Any<CancellationToken>()
                                 );
    }
}