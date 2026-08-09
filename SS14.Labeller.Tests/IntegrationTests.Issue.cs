using NSubstitute;
using NUnit.Framework;
using SS14.Labeller.Models;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using SS14.Labeller.Labelling.Labels;

namespace SS14.Labeller.Tests;

public partial class IntegrationTests
{
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

        await _applicationFactory.GitHubApiClient
                                 .Received()
                                 .AddLabel(
                                     Arg.Is<GithubRepo>(x => x.Name == "space-station-14" && x.Owner.Login == "Fildrance"),
                                     31,
                                     StatusLabel.Untriaged,
                                     Arg.Any<CancellationToken>()
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
    public async Task Issue_Labeled_IsValid_CreatesCopyInMainRepository()
    {
        // Arrange
        const string fileName = "issue_labeled_is_valid.json";
        var requestContent = await CreateRequestContent(fileName, "issues");

        const string linkToCreated = "https://github.com/Fildrance/space-station-14/issues/99";
        _applicationFactory.GitHubApiClient
                          .CreateIssue(
                              Arg.Any<string>(),
                              Arg.Any<string>(),
                              Arg.Is<GithubRepo>(x => x.Name == "space-station-14" && x.Owner.Login == "Fildrance"),
                              Arg.Any<CancellationToken>()
                          )
                          .Returns(linkToCreated);

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
                                 .CreateIssue(
                                     "test case 4",
                                     "test description\nrich format\n\n> a\n\n- points\n- points\n- points",
                                     Arg.Is<GithubRepo>(r=>r.Name == "space-station-14" && r.Owner.Login == "Fildrance"),
                                     Arg.Any<CancellationToken>()
                                 );

        await _applicationFactory.GitHubApiClient
                                 .Received()
                                 .AddComment(
                                     Arg.Is<GithubRepo>(x => x.Name == "Kaizen" && x.Owner.Login == "Fildrance"),
                                     38,
                                     Arg.Is<string>(comment => comment.Contains(linkToCreated)),
                                     Arg.Any<CancellationToken>()
                                 );
    }
}