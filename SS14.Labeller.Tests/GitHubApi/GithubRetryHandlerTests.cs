using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;
using SS14.Labeller.Configuration;
using SS14.Labeller.GitHubApi;

namespace SS14.Labeller.Tests.GitHubApi;

[Category("Unit")]
public class GithubRetryHandlerTests
{
    private readonly ILogger<GithubRetryHandler> _logger = Substitute.For<ILogger<GithubRetryHandler>>();
    private readonly IOptionsMonitor<GitHubConfig> _config = Substitute.For<IOptionsMonitor<GitHubConfig>>();
    private readonly MockHttpMessageHandler _mockInnerHandler = Substitute.ForPartsOf<MockHttpMessageHandler>();
    private HttpRequestMessage _httpRequestMessage = default!;
    private GitHubConfig _gitHubConfig;

    [SetUp]
    public void Setup()
    {
        _gitHubConfig = new GitHubConfig();
        _config.CurrentValue.Returns(_gitHubConfig);
        _httpRequestMessage = new HttpRequestMessage();
        _httpRequestMessage.RequestUri = new Uri("http://some-random-non-existing-uri.cam");
    }

    [Test]
    public void SendAsync_SuccessfulRequest()
    {
        // Arrange
        _mockInnerHandler.Send(Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>())
                         .Returns(Task.FromResult(new HttpResponseMessage { StatusCode = HttpStatusCode.OK }));

        var handler = new GithubRetryHandler(_mockInnerHandler, _config, _logger);
        var httpClient = new HttpClient(handler);

        // Act
        var result = httpClient.SendAsync(_httpRequestMessage, default).Result;

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
    }

    [Test]
    public void SendAsync_NetworkErrorRetries()
    {
        // Arrange
        _mockInnerHandler.Send(Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>())
                         .Returns(
                             _=> throw new HttpRequestException(HttpRequestError.ConnectionError), 
                             _=>  Task.FromResult(new HttpResponseMessage { StatusCode = HttpStatusCode.OK })
                        );

        var handler = new GithubRetryHandler(_mockInnerHandler, _config, _logger);
        var httpClient = new HttpClient(handler);

        // Act
        var result = httpClient.SendAsync(_httpRequestMessage, default).Result;

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
    }

    [Test]
    public void SendAsync_CalculateNextRequestTimeWithRateLimits()
    {
        // Arrange
        var response1 = new HttpResponseMessage(HttpStatusCode.TooManyRequests)
        {
            Headers =
            {
                { "X-Ratelimit-Remaining", "0" },
                { "X-Ratelimit-Reset", "170" }
            }
        };

        var response2 = new HttpResponseMessage(HttpStatusCode.OK);

        _mockInnerHandler.Send(Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>())
                         .Returns(response1, response2);

        var handler = new GithubRetryHandler(_mockInnerHandler, _config, _logger);
        var httpClient = new HttpClient(handler);

        // Act
        var sw = Stopwatch.StartNew();
        httpClient.SendAsync(_httpRequestMessage, default);
        sw.Stop();

        // Assert
        Assert.GreaterOrEqual(TimeSpan.FromSeconds(171), sw.Elapsed); // Добавляем дополнительное буферное время
    }

    [Test]
    public void SendAsync_MaxRetryExceeded()
    {
        // Arrange
        _mockInnerHandler.Send(Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>())
                         .ThrowsAsync(new HttpRequestException(HttpRequestError.ConnectionError));

        _gitHubConfig.MaxRetryAttempt = 2;

        var handler = new GithubRetryHandler(_mockInnerHandler, _config, _logger);
        var httpClient = new HttpClient(handler);

        // Act & Assert
        var sw = Stopwatch.StartNew();
        Assert.ThrowsAsync<HttpRequestException>(() =>
            httpClient.SendAsync(_httpRequestMessage, default));
        sw.Stop();

        Assert.That(sw.Elapsed, Is.GreaterThanOrEqualTo(TimeSpan.FromSeconds(2)));
    }
}

public class MockHttpMessageHandler : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return Send(request, cancellationToken);
    }

    public virtual Task<HttpResponseMessage> Send(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}