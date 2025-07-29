using NSubstitute;
using NUnit.Framework;
using SS14.Labeller.Models;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using SS14.Labeller.Labels;

namespace SS14.Labeller.Tests;

public partial class IntegrationTests
{
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
    public async Task PullRequest_ToStaging_ApplyStagingBranchLabel()
    {
        // Arrange
        const string fileName = "pull_request_staging.json";
        var requestContent = await CreateRequestContent(fileName, "pull_request");

        // Act
        await _client.PostAsync("/webhook", requestContent);

        // Assert
        _applicationFactory.GitHubApiClient
                           .Received()
                           .AddLabel(
                               Arg.Is<Repository>(x => x.Name == "SS14.Labeller" && x.Owner.Login == "Fildrance"),
                               4,
                               BranchLabels.Staging,
                               Arg.Any<CancellationToken>()
                           );
    }
    
    [Test]
    public async Task PullRequest_ToStable_ApplyStagingBranchLabel()
    {
        // Arrange
        const string fileName = "pull_request_stable.json";
        var requestContent = await CreateRequestContent(fileName, "pull_request");

        // Act
        await _client.PostAsync("/webhook", requestContent);

        // Assert
        _applicationFactory.GitHubApiClient
                           .Received()
                           .AddLabel(
                               Arg.Is<Repository>(x => x.Name == "SS14.Labeller" && x.Owner.Login == "Fildrance"),
                               4,
                               BranchLabels.Stable,
                               Arg.Any<CancellationToken>()
                           );
    }
    
    [Test]
    public async Task PullRequest_ToMaster_ApplyNoBranchLabel()
    {
        // Arrange
        const string fileName = "pull_request.json";
        var requestContent = await CreateRequestContent(fileName, "pull_request");

        // Act
        await _client.PostAsync("/webhook", requestContent);

        // Assert
        _applicationFactory.GitHubApiClient
                           .DidNotReceive()
                           .AddLabel(
                               Arg.Any<Repository>(),
                               Arg.Any<int>(),
                               BranchLabels.Stable,
                               Arg.Any<CancellationToken>()
                           );
        _applicationFactory.GitHubApiClient
                           .DidNotReceive()
                           .AddLabel(
                               Arg.Any<Repository>(),
                               Arg.Any<int>(),
                               BranchLabels.Staging,
                               Arg.Any<CancellationToken>()
                           );
    }

    [Test]
    public async Task PullRequest_ByMaintainer_ApplyApprovedLabel()
    {
        // Arrange
        const string fileName = "pull_request.json";
        var requestContent = await CreateRequestContent(fileName, "pull_request");

        _applicationFactory.GitHubApiClient.GetChangedFiles(
            Arg.Is<Repository>(x => x.Name == "SS14.Labeller" && x.Owner.Login == "Fildrance"),
            4,
            Arg.Any<CancellationToken>()
        ).Returns(["LootSystem.cs", "Loot.png",]);
        _applicationFactory.GitHubApiClient.GetPermission(
            Arg.Is<Repository>(x => x.Name == "SS14.Labeller" && x.Owner.Login == "Fildrance"),
            "Fildrance",
            Arg.Any<CancellationToken>()
        ).Returns(Task.FromResult("write"));

        // Act
        await _client.PostAsync("/webhook", requestContent);

        // Assert
        _applicationFactory.GitHubApiClient
                           .Received()
                           .AddLabel(
                               Arg.Is<Repository>(x => x.Name == "SS14.Labeller" && x.Owner.Login == "Fildrance"),
                               4,
                               StatusLabels.Approved,
                               Arg.Any<CancellationToken>()
                           );
    }

    [Test]
    public async Task PullRequest_ByNonMaintainer_ApplyRequireReviewLabel()
    {
        // Arrange
        const string fileName = "pull_request.json";
        var requestContent = await CreateRequestContent(fileName, "pull_request");

        _applicationFactory.GitHubApiClient.GetChangedFiles(
            Arg.Is<Repository>(x => x.Name == "SS14.Labeller" && x.Owner.Login == "Fildrance"),
            4,
            Arg.Any<CancellationToken>()
        ).Returns(["LootSystem.cs", "Loot.png",]);
        _applicationFactory.GitHubApiClient.GetPermission(
            Arg.Is<Repository>(x => x.Name == "SS14.Labeller" && x.Owner.Login == "Fildrance"),
            "Fildrance",
            Arg.Any<CancellationToken>()
        ).Returns(Task.FromResult("read"));

        // Act
        await _client.PostAsync("/webhook", requestContent);

        // Assert
        _applicationFactory.GitHubApiClient
                           .Received()
                           .AddLabel(
                               Arg.Is<Repository>(x => x.Name == "SS14.Labeller" && x.Owner.Login == "Fildrance"),
                               4,
                               StatusLabels.RequireReview,
                               Arg.Any<CancellationToken>()
                           );
    }

    [Test]
    public async Task PullRequest_Synchronize_SizeLabelInserted()
    {
        // Arrange
        const string fileName = "pull_request_synchonize_size.json";
        var requestContent = await CreateRequestContent(fileName, "pull_request");

        _applicationFactory.GitHubApiClient.GetChangedFiles(
            Arg.Is<Repository>(x => x.Name == "SS14.Labeller" && x.Owner.Login == "Fildrance"),
            4,
            Arg.Any<CancellationToken>()
        ).Returns(["LootSystem.cs", "Loot.png",]);

        // Act
        var result = await _client.PostAsync("/webhook", requestContent);

        // Assert
        _applicationFactory.GitHubApiClient
                           .Received()
                           .AddLabel(
                               Arg.Is<Repository>(x => x.Name == "SS14.Labeller" && x.Owner.Login == "Fildrance"),
                               4,
                               "size/L",
                               Arg.Any<CancellationToken>()
                           );

        _applicationFactory.GitHubApiClient
                           .Received()
                           .RemoveLabel(
                               Arg.Is<Repository>(x => x.Name == "SS14.Labeller" && x.Owner.Login == "Fildrance"),
                               4,
                               "size/S",
                               Arg.Any<CancellationToken>()
                           );
    }

    [Test]
    public async Task PullRequest_Synchronize_ContentLabelSet()
    {
        // Arrange
        const string fileName = "pull_request_synchonize.json";
        var requestContent = await CreateRequestContent(fileName, "pull_request");

        _applicationFactory.GitHubApiClient.GetChangedFiles(
            Arg.Is<Repository>(x => x.Name == "SS14.Labeller" && x.Owner.Login == "Fildrance"),
            4,
            Arg.Any<CancellationToken>()
        ).Returns(["LootSystem.cs", "MyLoots.rsi/Loot.png", "PickLoot.ogg"]);

        // Act
        var result = await _client.PostAsync("/webhook", requestContent);

        // Assert
        _applicationFactory.GitHubApiClient
                           .Received()
                           .AddLabel(
                               Arg.Is<Repository>(x => x.Name == "SS14.Labeller" && x.Owner.Login == "Fildrance"),
                               4,
                               ChangesLabels.Sprites,
                               Arg.Any<CancellationToken>()
                           );

        _applicationFactory.GitHubApiClient
                           .Received()
                           .AddLabel(
                               Arg.Is<Repository>(x => x.Name == "SS14.Labeller" && x.Owner.Login == "Fildrance"),
                               4,
                               ChangesLabels.Audio,
                               Arg.Any<CancellationToken>()
                           );

        _applicationFactory.GitHubApiClient
                           .DidNotReceive()
                           .AddLabel(
                               Arg.Is<Repository>(x => x.Name == "SS14.Labeller" && x.Owner.Login == "Fildrance"),
                               4,
                               ChangesLabels.NoCSharp,
                               Arg.Any<CancellationToken>()
                           );

        _applicationFactory.GitHubApiClient
                           .DidNotReceive()
                           .RemoveLabel(
                               Arg.Any<Repository>(),
                               Arg.Any<int>(),
                               Arg.Any<string>(),
                               Arg.Any<CancellationToken>()
                           );
    }

    [Test]
    public async Task PullRequest_SynchronizeNoCSharp_NoCSharpLabelSet()
    {
        // Arrange
        const string fileName = "pull_request_synchonize.json";
        var requestContent = await CreateRequestContent(fileName, "pull_request");

        _applicationFactory.GitHubApiClient.GetChangedFiles(
            Arg.Is<Repository>(x => x.Name == "SS14.Labeller" && x.Owner.Login == "Fildrance"),
            4,
            Arg.Any<CancellationToken>()
        ).Returns(["MyLoots.rsi/Loot.png", "PickLoot.ogg", "shaders/swag.swsl", "Resources/Maps/main.ylm", "Resources/PrototypesMaps/main.ylm", "loot-picker.xaml"]);

        // Act
        var result = await _client.PostAsync("/webhook", requestContent);

        // Assert
        _applicationFactory.GitHubApiClient
                           .Received()
                           .AddLabel(
                               Arg.Is<Repository>(x => x.Name == "SS14.Labeller" && x.Owner.Login == "Fildrance"),
                               4,
                               ChangesLabels.NoCSharp,
                               Arg.Any<CancellationToken>()
                           );

        _applicationFactory.GitHubApiClient
                           .DidNotReceive()
                           .RemoveLabel(
                               Arg.Any<Repository>(),
                               Arg.Any<int>(),
                               Arg.Any<string>(),
                               Arg.Any<CancellationToken>()
                           );
    }
}