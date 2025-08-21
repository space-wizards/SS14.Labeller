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
        await _applicationFactory.GitHubApiClient
                                 .Received()
                                 .AddLabel(
                                     Arg.Is<GithubRepo>(x => x.Name == "SS14.Labeller" && x.Owner.Login == "Fildrance"),
                                     4,
                                     BranchLabel.Staging,
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
        await _applicationFactory.GitHubApiClient
                                 .Received()
                                 .AddLabel(
                                     Arg.Is<GithubRepo>(x => x.Name == "SS14.Labeller" && x.Owner.Login == "Fildrance"),
                                     4,
                                     BranchLabel.Stable,
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
        await _applicationFactory.GitHubApiClient
                                 .DidNotReceive()
                                 .AddLabel(
                                     Arg.Any<GithubRepo>(),
                                     Arg.Any<int>(),
                                     BranchLabel.Stable,
                                     Arg.Any<CancellationToken>()
                                 );
        await _applicationFactory.GitHubApiClient
                                 .DidNotReceive()
                                 .AddLabel(
                                     Arg.Any<GithubRepo>(),
                                     Arg.Any<int>(),
                                     BranchLabel.Staging,
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
            Arg.Is<GithubRepo>(x => x.Name == "SS14.Labeller" && x.Owner.Login == "Fildrance"),
            4,
            Arg.Any<CancellationToken>()
        ).Returns(["LootSystem.cs", "Loot.png",]);
        _applicationFactory.GitHubApiClient.IsMaintainer(
            "Fildrance",
            Arg.Is<GithubRepo>(x => x.Name == "SS14.Labeller" && x.Owner.Login == "Fildrance"),
            Arg.Any<CancellationToken>()
        ).Returns(Task.FromResult(true));

        // Act
        await _client.PostAsync("/webhook", requestContent);

        // Assert
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
    public async Task PullRequest_ByNonMaintainer_ApplyRequireReviewLabel()
    {
        // Arrange
        const string fileName = "pull_request.json";
        var requestContent = await CreateRequestContent(fileName, "pull_request");

        _applicationFactory.GitHubApiClient.GetChangedFiles(
            Arg.Is<GithubRepo>(x => x.Name == "SS14.Labeller" && x.Owner.Login == "Fildrance"),
            4,
            Arg.Any<CancellationToken>()
        ).Returns(["LootSystem.cs", "Loot.png",]);
        _applicationFactory.GitHubApiClient.IsMaintainer(
            "Fildrance",
            Arg.Is<GithubRepo>(x => x.Name == "SS14.Labeller" && x.Owner.Login == "Fildrance"),
            Arg.Any<CancellationToken>()
        ).Returns(Task.FromResult(true));

        // Act
        await _client.PostAsync("/webhook", requestContent);

        // Assert
        await _applicationFactory.GitHubApiClient
                                 .Received()
                                 .AddLabel(
                                     Arg.Is<GithubRepo>(x => x.Name == "SS14.Labeller" && x.Owner.Login == "Fildrance"),
                                     4,
                                     StageOfWorkLabel.RequireReview,
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
            Arg.Is<GithubRepo>(x => x.Name == "SS14.Labeller" && x.Owner.Login == "Fildrance"),
            4,
            Arg.Any<CancellationToken>()
        ).Returns(["LootSystem.cs", "Loot.png",]);

        // Act
        var result = await _client.PostAsync("/webhook", requestContent);

        // Assert
        await _applicationFactory.GitHubApiClient
                                 .Received()
                                 .AddLabel(
                                     Arg.Is<GithubRepo>(x => x.Name == "SS14.Labeller" && x.Owner.Login == "Fildrance"),
                                     4,
                                     SizeLabel.L,
                                     Arg.Any<CancellationToken>()
                                 );

        await _applicationFactory.GitHubApiClient
                                 .Received()
                                 .RemoveLabel(
                                     Arg.Is<GithubRepo>(x => x.Name == "SS14.Labeller" && x.Owner.Login == "Fildrance"),
                                     4,
                                     SizeLabel.S,
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
            Arg.Is<GithubRepo>(x => x.Name == "SS14.Labeller" && x.Owner.Login == "Fildrance"),
            4,
            Arg.Any<CancellationToken>()
        ).Returns(["LootSystem.cs", "MyLoots.rsi/Loot.png", "PickLoot.ogg"]);

        // Act
        var result = await _client.PostAsync("/webhook", requestContent);

        // Assert
        await _applicationFactory.GitHubApiClient
                                 .Received()
                                 .AddLabel(
                                     Arg.Is<GithubRepo>(x => x.Name == "SS14.Labeller" && x.Owner.Login == "Fildrance"),
                                     4,
                                     ChangesLabel.Sprites,
                                     Arg.Any<CancellationToken>()
                                 );

        await _applicationFactory.GitHubApiClient
                                 .Received()
                                 .AddLabel(
                                     Arg.Is<GithubRepo>(x => x.Name == "SS14.Labeller" && x.Owner.Login == "Fildrance"),
                                     4,
                                     ChangesLabel.Audio,
                                     Arg.Any<CancellationToken>()
                                 );

        await _applicationFactory.GitHubApiClient
                                 .DidNotReceive()
                                 .AddLabel(
                                     Arg.Is<GithubRepo>(x => x.Name == "SS14.Labeller" && x.Owner.Login == "Fildrance"),
                                     4,
                                     ChangesLabel.NoCSharp,
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
    public async Task PullRequest_SynchronizeNoCSharp_NoCSharpLabelSet()
    {
        // Arrange
        const string fileName = "pull_request_synchonize.json";
        var requestContent = await CreateRequestContent(fileName, "pull_request");

        _applicationFactory.GitHubApiClient.GetChangedFiles(
            Arg.Is<GithubRepo>(x => x.Name == "SS14.Labeller" && x.Owner.Login == "Fildrance"),
            4,
            Arg.Any<CancellationToken>()
        ).Returns(["MyLoots.rsi/Loot.png", "PickLoot.ogg", "shaders/swag.swsl", "Resources/Maps/main.ylm", "Resources/PrototypesMaps/main.ylm", "loot-picker.xaml"]);

        // Act
        var result = await _client.PostAsync("/webhook", requestContent);

        // Assert
        await _applicationFactory.GitHubApiClient
                                 .Received()
                                 .AddLabel(
                                     Arg.Is<GithubRepo>(x => x.Name == "SS14.Labeller" && x.Owner.Login == "Fildrance"),
                                     4,
                                     ChangesLabel.NoCSharp,
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