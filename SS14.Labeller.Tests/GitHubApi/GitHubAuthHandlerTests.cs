using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;
using SS14.Labeller.Configuration;
using SS14.Labeller.GitHubApi;
using SS14.Labeller.Tests.GitHubApi.Mocks;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace SS14.Labeller.Tests.GitHubApi;

[Category("Unit"), ExcludeFromCodeCoverage]
public class GitHubAuthHandlerTests
{
    private IGitHubTokenProvider _tokenProvider = default!;
    private ILogger<GitHubAuthHandler> _logger = default!;
    private MockInnerHandler _innerHandler = default!;
    private IOptions<GitHubConfig> _options;
    private GitHubAuthHandler _cut;

    [SetUp]
    public void Setup()
    {
        var config = new GitHubConfig
        {
            Owner = "space-wizards",
            Repo = "space-station-14"
        };
        _options = Options.Create(config);

        _tokenProvider = Substitute.For<IGitHubTokenProvider>();
        _logger = Substitute.For<ILogger<GitHubAuthHandler>>();
        _innerHandler = new MockInnerHandler();

        _cut = new GitHubAuthHandler(_tokenProvider, _options, _logger)
        {
            InnerHandler = _innerHandler
        };
    }

    [Test]
    public async Task SendAsync_SetsBearerTokenAndForwards()
    {
        // Arrange
        _tokenProvider.GetTokenAsync("space-wizards", "space-station-14", Arg.Any<CancellationToken>())
                      .Returns("test-token");

        var client = new HttpClient(_cut);

        // Act
        var response = await client.GetAsync("https://api.github.com/repos/owner/repo/issues/1");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(_innerHandler.LastRequest, Is.Not.Null);
        Assert.That(_innerHandler.LastRequest!.Headers.Authorization!.Scheme, Is.EqualTo("Bearer"));
        Assert.That(_innerHandler.LastRequest!.Headers.Authorization!.Parameter, Is.EqualTo("test-token"));
    }

    [Test]
    public async Task SendAsync_UsesConfiguredOwnerAndRepo()
    {
        // Arrange
        string? capturedOwner = null;
        string? capturedRepo = null;
        _tokenProvider.GetTokenAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                      .Returns(x =>
                      {
                          capturedOwner = x.ArgAt<string>(0);
                          capturedRepo = x.ArgAt<string>(1);
                          return "token";
                      });

        var client = new HttpClient(_cut);

        // Act
        await client.GetAsync("https://api.github.com/app");

        // Assert
        Assert.That(capturedOwner, Is.EqualTo("space-wizards"));
        Assert.That(capturedRepo, Is.EqualTo("space-station-14"));
    }

    [Test]
    public async Task SendAsync_Unauthorized_InvalidatesTokenAndRetriesOnce()
    {
        // Arrange
        _tokenProvider.GetTokenAsync("space-wizards", "space-station-14", Arg.Any<CancellationToken>())
                      .Returns("old-token", "new-token");
        _innerHandler.Responses.Enqueue(new HttpResponseMessage(HttpStatusCode.Unauthorized));
        _innerHandler.Responses.Enqueue(new HttpResponseMessage(HttpStatusCode.OK));

        var client = new HttpClient(_cut);

        // Act
        var response = await client.GetAsync("https://api.github.com/repos/owner/repo/issues/1");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(_innerHandler.AuthTokens, Is.EqualTo(new[] { "old-token", "new-token" }));
        _tokenProvider.Received(1).InvalidateToken();
    }

    [Test]
    public async Task SendAsync_UnauthorizedTwice_ReturnsSecond401WithoutFurtherRetry()
    {
        // Arrange
        _tokenProvider.GetTokenAsync("space-wizards", "space-station-14", Arg.Any<CancellationToken>())
                      .Returns("token-a", "token-b");
        _innerHandler.Responses.Enqueue(new HttpResponseMessage(HttpStatusCode.Unauthorized));
        _innerHandler.Responses.Enqueue(new HttpResponseMessage(HttpStatusCode.Unauthorized));

        var client = new HttpClient(_cut);

        // Act
        var response = await client.GetAsync("https://api.github.com/repos/owner/repo/issues/1");

        // Assert — one retry only, then the 401 is returned to the caller.
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        Assert.That(_innerHandler.AuthTokens.Count, Is.EqualTo(2));
    }

    [Test]
    public async Task SendAsync_Forbidden_DoesNotInvalidateOrRetry()
    {
        // Arrange
        _tokenProvider.GetTokenAsync("space-wizards", "space-station-14", Arg.Any<CancellationToken>())
                      .Returns("token");
        _innerHandler.Responses.Enqueue(new HttpResponseMessage(HttpStatusCode.Forbidden));

        var client = new HttpClient(_cut);

        // Act
        var response = await client.GetAsync("https://api.github.com/repos/owner/repo/issues/1");

        // Assert — only auth failures (401) trigger token invalidation + retry.
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
        Assert.That(_innerHandler.AuthTokens.Count, Is.EqualTo(1));
        _tokenProvider.DidNotReceive().InvalidateToken();
    }

}
