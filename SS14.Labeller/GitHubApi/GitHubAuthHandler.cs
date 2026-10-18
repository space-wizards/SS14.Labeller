using System.Net;
using System.Net.Http.Headers;
using Microsoft.Extensions.Options;
using SS14.Labeller.Configuration;

namespace SS14.Labeller.GitHubApi;

/// <summary>
/// Applies a bearer token from <see cref="IGitHubTokenProvider"/> to every outgoing request.
/// <br/>
/// On a <c>401 Unauthorized</c> response, invalidates the token provider's cached token and
/// retries the request once with a freshly acquired token.
/// </summary>
public sealed class GitHubAuthHandler(
    IGitHubTokenProvider tokenProvider,
    IOptions<GitHubConfig> config,
    ILogger<GitHubAuthHandler> logger
) : DelegatingHandler
{
    private readonly GitHubConfig _config = config.Value;

    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = await SendWithTokenAsync(request, cancellationToken);
        if (response.StatusCode != HttpStatusCode.Unauthorized)
            return response;

        // Token was rejected (e.g. revoked installation token). Invalidate and retry once with a fresh one.
        tokenProvider.InvalidateToken();
        logger.LogWarning(
            "GitHub API returned 401 Unauthorized for {Method} {Path}. Invalidating auth token and retrying once.",
            request.Method,
            request.RequestUri?.AbsolutePath);

        response.Dispose();

        return await SendWithTokenAsync(request, cancellationToken);
    }

    private async Task<HttpResponseMessage> SendWithTokenAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await tokenProvider.GetTokenAsync(_config.Owner, _config.Repo, cancellationToken);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await base.SendAsync(request, cancellationToken);
    }
}
