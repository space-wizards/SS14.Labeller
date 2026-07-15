using NSubstitute;
using NUnit.Framework;
using SS14.Labeller.Messages;
using System.Threading;
using System.Threading.Tasks;

namespace SS14.Labeller.Tests;

public partial class IntegrationTests
{
    [Test]
    public async Task PullRequestMerge_NonEmpty_CreatesBreakingChangesTopic()
    {
        // Arrange
        const string fileName = "pull_request_merge_with_breaking_changes.json";
        var requestContent = await CreateRequestContent(fileName, "pull_request");

        // Act
        var result = await _client.PostAsync("/webhook", requestContent);

        // Assert
        const string url = "https://github.com/Fildrance/Kaizen/pull/35";
        const string bc = "`VaporComponent` `SolutionPurgeComponent` and `SolutionRegenerationComponent` now exist on the Solution Entities themselves and no longer do a solutions lookup. " 
                          + "If you need these behaviors on your solution entity, attach them to the relevant solution prototype.";
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
    
    [Test]
    public async Task PullRequestMerge_Empty_DoesNotCreateTopic()
    {
        // Arrange
        const string fileName = "pull_request_merge_with_empty_breaking_changes.json";
        var requestContent = await CreateRequestContent(fileName, "pull_request");

        // Act
        var result = await _client.PostAsync("/webhook", requestContent);

        // Assert
        await _applicationFactory.DiscourseClient
                                 .DidNotReceive()
                                 .CreateTopic(
                                     Arg.Any<int>(),
                                     Arg.Any<string>(),
                                     Arg.Any<string>(),
                                     Arg.Any<CancellationToken>()
                                 );
    }
    
    [Test]
    public async Task PullRequestMerge_ContainsOnlySpecialCharacters_DoesNotCreateTopic()
    {
        // Arrange
        const string fileName = "pull_request_merge_with_special_symbols_only_breaking_changes.json";
        var requestContent = await CreateRequestContent(fileName, "pull_request");

        // Act
        var result = await _client.PostAsync("/webhook", requestContent);

        // Assert
        await _applicationFactory.DiscourseClient
                                 .DidNotReceive()
                                 .CreateTopic(
                                     Arg.Any<int>(),
                                     Arg.Any<string>(),
                                     Arg.Any<string>(),
                                     Arg.Any<CancellationToken>()
                                 );
    }
}