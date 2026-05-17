using NSubstitute;
using NUnit.Framework;
using SS14.Labeller.Messages;
using SS14.Labeller.Models;
using System.Threading;
using System.Threading.Tasks;

namespace SS14.Labeller.Tests;

public partial class IntegrationTests
{
    [Test]
    public async Task PullRequestMergeCreatesBreakingChangesTopic()
    {
        // Arrange
        const string fileName = "pull_request_merge_with_breaking_changes.json";
        var requestContent = await CreateRequestContent(fileName, "pull_request");

        // Act
        var result = await _client.PostAsync("/webhook", requestContent);

        // Assert
        const string url = "https://github.com/Fildrance/Kaizen/pull/35";
        const string bc = "`VaporComponent` `SolutionPurgeComponent` and `SolutionRegenerationComponent` now exist on the Solution Entities themselves and no longer do a solutions lookup. If you need these behaviors on your solution entity, attach them to the relevant solution prototype.";
        const string title = "test 3";
        await _applicationFactory.DiscourseClient
                                 .Received()
                                 .CreateTopic(
                                     1337,
                                     StatusMessages.BreakingChangesTopicBody(url, bc),
                                     title,
                                     Arg.Any<CancellationToken>()
                                 );
    }
}