using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;
using SS14.Labeller.Configuration;
using SS14.Labeller.GitHubApi;
using SS14.Labeller.GitHubApi.GitHubAppIntegration;
using SS14.Labeller.Tests.GitHubApi.Mocks;

namespace SS14.Labeller.Tests.GitHubApi;

[Category("Unit"), ExcludeFromCodeCoverage]
public class GitHubAppTokenProviderTests
{
    private HttpClient _httpClient = default!;
    private MockInnerHandler _messageHandler = default!;
    private ILogger<GitHubAppTokenProvider> _logger;
    private IOptions<GitHubConfig> _options;
    private IGitHubTokenProvider _cut;

    [SetUp]
    public void Setup()
    {
        using var rsa = RSA.Create(2048);
        var config = new GitHubConfig
        {
            AuthMode = GitHubAuthMode.App,
            AppId = 123456,
            AppPrivateKey = rsa.ExportPkcs8PrivateKeyPem(),
            Owner = "space-wizards",
            Repo = "space-station-14"
        };
        _options = Options.Create(config);
        _messageHandler = new MockInnerHandler();
        _logger = Substitute.For<ILogger<GitHubAppTokenProvider>>();

        _httpClient = new HttpClient(_messageHandler);

        _cut = new GitHubAppTokenProvider(_httpClient, _options, _logger);
    }

    [Test]
    public async Task GetToken_SuccessfulResponse_CachesToken()
    {
        // Arrange
        _messageHandler.Responses.Enqueue(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = ToContent("""{"id":123,"account":{"login":"owner"}}""")
        });
        _messageHandler.Responses.Enqueue(new HttpResponseMessage(HttpStatusCode.Created)
        {
            Content = ToContent("""{"token":"token-abc","expires_at":"2999-01-01T00:00:00Z"}""")
        });

        // Act 
        var token1 = await _cut.GetTokenAsync("space-wizards", "space-station-14", CancellationToken.None);
        var token2 = await _cut.GetTokenAsync("space-wizards", "space-station-14", CancellationToken.None);

        // Assert
        Assert.That(token1, Is.EqualTo("token-abc"));
        Assert.That(token2, Is.EqualTo("token-abc"));
        Assert.That(_messageHandler.Requests.Count, Is.EqualTo(2), "Discovery + one token fetch, then cached.");
    }

    [Test]
    public async Task GetToken_ExpiredToken_FetchesNewTokenWithoutRediscovery()
    {
        // Arrange
        _messageHandler.Responses.Enqueue(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = ToContent("""{"id":123,"account":{"login":"owner"}}""")
        });
        _messageHandler.Responses.Enqueue(new HttpResponseMessage(HttpStatusCode.Created)
        {
            Content = ToContent("""{"token":"old-token","expires_at":"2000-01-01T00:00:00Z"}""")
        });
        _messageHandler.Responses.Enqueue(new HttpResponseMessage(HttpStatusCode.Created)
        {
            Content = ToContent("""{"token":"new-token","expires_at":"2999-01-01T00:00:00Z"}""")
        });

        // Act
        var token1 = await _cut.GetTokenAsync("space-wizards", "space-station-14", CancellationToken.None);
        var token2 = await _cut.GetTokenAsync("space-wizards", "space-station-14", CancellationToken.None);

        // Assert
        Assert.That(token1, Is.EqualTo("old-token"));
        Assert.That(token2, Is.EqualTo("new-token"));
        Assert.That(_messageHandler.Requests.Count, Is.EqualTo(3), "Discovery once, then two token fetches.");
    }

    [Test]
    public async Task GetToken_ResolvesInstallationFromRepo()
    {
        // Arrange
        _messageHandler.Responses.Enqueue(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = ToContent("""{"id":123,"account":{"login":"owner"}}""")
        });
        _messageHandler.Responses.Enqueue(new HttpResponseMessage(HttpStatusCode.Created)
        {
            Content = ToContent("""{"token":"resolved-token","expires_at":"2999-01-01T00:00:00Z"}""")
        });

        // Act
        var token = await _cut.GetTokenAsync("space-wizards", "space-station-14", CancellationToken.None);

        // Assert
        Assert.That(token, Is.EqualTo("resolved-token"));
        Assert.That(_messageHandler.Requests.Count, Is.EqualTo(2));
        var resolveRequest = _messageHandler.Requests[0];
        Assert.That(resolveRequest.RequestUri!.AbsolutePath, Is.EqualTo("/repos/space-wizards/space-station-14/installation"));
        var tokenRequest = _messageHandler.Requests[1];
        Assert.That(tokenRequest.RequestUri!.AbsolutePath, Is.EqualTo("/app/installations/123/access_tokens"));
    }

    [Test]
    public void GetToken_RequestUsesJwtBearerAuth()
    {
        // Arrange
        _messageHandler.Responses.Enqueue(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = ToContent("""{"id":123,"account":{"login":"owner"}}""")
        });
        _messageHandler.Responses.Enqueue(new HttpResponseMessage(HttpStatusCode.Unauthorized));

        // Act Assert
        var ex = Assert.ThrowsAsync<HttpRequestException>(() =>
            _cut.GetTokenAsync("space-wizards", "space-station-14", CancellationToken.None));

        Assert.That(ex!.Message, Does.Contain("401"));
        var request = _messageHandler.Requests[1];
        Assert.That(request.RequestUri!.AbsolutePath, Is.EqualTo("/app/installations/123/access_tokens"));

        var auth = request.Headers.Authorization!;
        Assert.That(auth.Scheme, Is.EqualTo("Bearer"));
        var jwt = auth.Parameter!;
        var parts = jwt.Split('.');
        Assert.That(parts, Has.Length.EqualTo(3));

        var payloadJson = Base64UrlDecode(parts[1]);
        using var doc = System.Text.Json.JsonDocument.Parse(payloadJson);
        Assert.That(doc.RootElement.GetProperty("iss").GetInt64(), Is.EqualTo(123456));
    }

    private HttpContent ToContent(string text)
    {
        return new StringContent(text, Encoding.UTF8, "application/json");
    }

    private static string Base64UrlDecode(string input)
    {
        var padded = input.Replace('-', '+').Replace('_', '/');
        padded = padded.PadRight(padded.Length + (4 - padded.Length % 4) % 4, '=');
        return Encoding.UTF8.GetString(Convert.FromBase64String(padded));
    }

    [TearDown]
    public void Teardown()
    {
        _cut.Dispose();
    }
}
