using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using NSubstitute;
using NUnit.Framework;
using SS14.Labeller.Helpers;
using SS14.Labeller.Labels;
using SS14.Labeller.Models;

namespace SS14.Labeller.Tests;

[ExcludeFromCodeCoverage]
public class IntegrationTests
{
    private const string HookSecret = "asdasdasdasdasdasdasdadsadad";

    private CustomWebApplicationFactory _applicationFactory;

    private HttpClient _client;

    [SetUp]
    public void Setup()
    {
        Environment.SetEnvironmentVariable("GITHUB_WEBHOOK_SECRET", HookSecret);
        Environment.SetEnvironmentVariable("GITHUB_TOKEN", "DUMMY");


        _applicationFactory = new CustomWebApplicationFactory();
        _client = _applicationFactory.CreateClient();
    }

    [Test]
    public async Task Ping()
    {
        var result = await _client.GetAsync("/");
        var content = await result.Content.ReadAsStringAsync();
        Assert.That(content.Contains("nik", StringComparison.InvariantCultureIgnoreCase));
        Assert.That(content.Contains("cat", StringComparison.InvariantCultureIgnoreCase));
    }

    [Test]
    public async Task PullRequest()
    {
        // Arrange
        const string fileName = "pull_request.json";
        var requestContent = await CreateRequestContent(fileName, "pull_request");

        // Act
        var result = await _client.PostAsync("/webhook", requestContent);

        // Assert
        var respText = await result.Content.ReadAsStringAsync();
        Assert.That(
            result.StatusCode,
            Is.EqualTo(HttpStatusCode.NoContent), 
            $"Invalid response status - {result.StatusCode}, response text: \r\n{respText}."
        ); 

    }

    [Test]
    public async Task PullRequestReview_ReviewByNonMaintainer_DoNothing()
    {
        // Arrange
        const string fileName = "pull_request_review_approve.json";
        var requestContent = await CreateRequestContent(fileName, "pull_request_review");

        _applicationFactory.GitHubApiClient.GetPermission(
            Arg.Is<Repository>(x => x.Name == "SS14.Labeller" && x.Owner.Login == "Fildrance"),
            "NonFildrance"
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

        _applicationFactory.GitHubApiClient
                           .DidNotReceive()
                           .AddLabel(
                               Arg.Any<Repository>(),
                               Arg.Any<int>(),
                               Arg.Any<string>()
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
            "Fildrance"
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

        _applicationFactory.GitHubApiClient
                           .Received()
                           .AddLabel(
                               Arg.Is<Repository>(x => x.Name == "SS14.Labeller" && x.Owner.Login == "Fildrance"),
                               4,
                               StatusLabels.Approved
                           );

        _applicationFactory.GitHubApiClient
                           .Received()
                           .RemoveLabel(
                               Arg.Is<Repository>(x => x.Name == "SS14.Labeller" && x.Owner.Login == "Fildrance"),
                               4,
                               StatusLabels.RequireReview
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
            "Fildrance"
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

        _applicationFactory.GitHubApiClient
                           .Received()
                           .AddLabel(
                               Arg.Is<Repository>(x => x.Name == "SS14.Labeller" && x.Owner.Login == "Fildrance"),
                               4,
                               StatusLabels.AwaitingChanges
                           );

        _applicationFactory.GitHubApiClient
                           .Received()
                           .RemoveLabel(
                               Arg.Is<Repository>(x => x.Name == "SS14.Labeller" && x.Owner.Login == "Fildrance"),
                               4,
                               StatusLabels.RequireReview
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
            "Fildrance"
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

        _applicationFactory.GitHubApiClient
                           .DidNotReceive()
                           .AddLabel(
                               Arg.Any<Repository>(),
                               Arg.Any<int>(),
                               Arg.Any<string>()
                           );

        _applicationFactory.GitHubApiClient
                           .DidNotReceive()
                           .RemoveLabel(
                               Arg.Any<Repository>(),
                               Arg.Any<int>(),
                               Arg.Any<string>()
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
            "Fildrance"
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

        _applicationFactory.GitHubApiClient
                           .DidNotReceive()
                           .AddLabel(
                               Arg.Any<Repository>(),
                               Arg.Any<int>(),
                               Arg.Any<string>()
                           );

        _applicationFactory.GitHubApiClient
                           .DidNotReceive()
                           .RemoveLabel(
                               Arg.Any<Repository>(),
                               Arg.Any<int>(),
                               Arg.Any<string>()
                           );
    }

    [Test]
    public async Task Issue_Created_AddedUntriagedLabel()
    {
        // Arrange
        const string fileName = "issue_created.json";
        var requestContent = await CreateRequestContent(fileName, "issues");

        // Act
        var result = await _client.PostAsync("/webhook", requestContent);

        // Assert
        var respText = await result.Content.ReadAsStringAsync();
        Assert.That(
            result.StatusCode,
            Is.EqualTo(HttpStatusCode.NoContent), 
            $"Invalid response status - {result.StatusCode}, response text: \r\n{respText}."
        );

        _applicationFactory.GitHubApiClient
                           .Received()
                           .AddLabel(
                               Arg.Is<Repository>(x => x.Name == "Kaizen" && x.Owner.Login == "Fildrance"),
                               31,
                               StatusLabels.Untriaged
                           );
    }
    [Test]
    public async Task Issue_Closed_NoLabelsAssigned()
    {
        // Arrange
        const string fileName = "issue_closed.json";
        var requestContent = await CreateRequestContent(fileName, "issues");

        // Act
        var result = await _client.PostAsync("/webhook", requestContent);

        // Assert
        var respText = await result.Content.ReadAsStringAsync();
        Assert.That(
            result.StatusCode,
            Is.EqualTo(HttpStatusCode.NoContent), 
            $"Invalid response status - {result.StatusCode}, response text: \r\n{respText}."
        );

        _applicationFactory.GitHubApiClient
                           .DidNotReceive()
                           .AddLabel(
                               Arg.Any<Repository>(),
                               Arg.Any<int>(),
                               Arg.Any<string>()
                           );
    }

    private static async Task<StringContent> CreateRequestContent(string requestFileName, string eventType)
    {
        var filePath = Path.Combine("Resources", requestFileName);
        var text = await File.ReadAllTextAsync(filePath);
        var requestContent = new StringContent(text, Encoding.UTF8);

        var msgHash = EncodingHelper.ToHmacSha256(Encoding.UTF8.GetBytes(text), HookSecret);
        requestContent.Headers.Add("X-Hub-Signature-256", $"sha256={msgHash}");
        requestContent.Headers.Add("X-GitHub-Event", eventType);
        return requestContent;
    }

    [TearDown]
    public void TearDown()
    {
        _applicationFactory.Dispose();
    }
}