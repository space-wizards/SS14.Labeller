﻿using System.Net;
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
                                     Arg.Is<GithubRepo>(x => x.Name == "Kaizen" && x.Owner.Login == "Fildrance"),
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
}