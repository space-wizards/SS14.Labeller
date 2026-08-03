using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using SS14.Labeller.Configuration;

namespace SS14.Labeller.GitHubApi.GitHubAppIntegration;

/// <summary>
/// Provides installation access tokens for a GitHub App.
/// <br/>
/// See <see href="https://docs.github.com/en/apps/creating-github-apps/authenticating-with-a-github-app/authenticating-as-a-github-app-installation">
/// Authenticating as a GitHub App installation
/// </see>.
/// </summary>
public sealed class GitHubAppTokenProvider : IGitHubTokenProvider
{
    private static readonly TimeSpan TokenExpiryBuffer = TimeSpan.FromSeconds(60);

    private readonly GitHubConfig _config;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private readonly string _baseUrl;

    private string? _installationToken;
    private DateTimeOffset? _installationTokenExpiry;
    private long? _resolvedInstallationId;
    private readonly HttpClient _httpClient;
    private readonly ILogger<GitHubAppTokenProvider> _logger;

    private readonly RSA _rsa;

    public GitHubAppTokenProvider(HttpClient httpClient, IOptions<GitHubConfig> config, ILogger<GitHubAppTokenProvider> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _config = config.Value;
        _baseUrl = _config.BaseUrl;

        _rsa = RSA.Create();
        _rsa.ImportFromPem(_config.AppPrivateKey);
    }

    /// <inheritdoc />
    public async Task<string> GetTokenAsync(string owner, string repo, CancellationToken ct)
    {
        // Fast path: cached token that is still valid.
        if (IsTokenValid())
            return _installationToken;

        await _refreshLock.WaitAsync(ct);
        try
        {
            // Re-check inside the lock in case another call refreshed while we were waiting.
            if (IsTokenValid())
                return _installationToken;

            return await RefreshTokenAsync(owner, repo, ct);
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    [MemberNotNullWhen(true, nameof(_installationToken))]
    private bool IsTokenValid()
    {
        return _installationToken != null 
               && _installationTokenExpiry > DateTimeOffset.UtcNow + TokenExpiryBuffer;
    }

    private async Task<string> RefreshTokenAsync(string owner, string repo, CancellationToken ct)
    {
        var installationId = _resolvedInstallationId ??= await ResolveInstallationIdAsync(owner, repo, ct);

        var jwt = CreateAppJwt();
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/app/installations/{installationId}/access_tokens");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

        using var response = await _httpClient.SendAsync(request, ct);
        var content = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to get App installation access token, response: \r\n {content}", content);
            throw new HttpRequestException($"Failed to get GitHub App installation access token. Status: {(int)response.StatusCode}");
        }

        using var document = JsonDocument.Parse(content);
        var root = document.RootElement;
        string? token;
        if (!root.TryGetProperty("token", out var tokenProperty)
            || string.IsNullOrWhiteSpace(token = tokenProperty.GetString())
            || !root.TryGetProperty("expires_at", out var expiresAtProperty)
            || !expiresAtProperty.TryGetDateTimeOffset(out var expiresAt)
        )
        {
            _logger.LogError("Failed parse App installation access token response: \r\n {content}", content);
            throw new JsonException("GitHub App installation access token response missing required fields.");
        }

        _installationToken = token;
        _installationTokenExpiry = expiresAt;

        return token;
    }

    /// <summary>
    /// Resolves the installation ID for a repository using the JWT-authenticated App identity.
    /// <br/>
    /// See
    /// <see href="https://docs.github.com/en/rest/apps/apps?apiVersion=2022-11-28#get-a-repository-installation-for-the-authenticated-app">
    /// Get a repository installation for the authenticated app
    /// </see>.
    /// </summary>
    private async Task<long> ResolveInstallationIdAsync(string owner, string repo, CancellationToken ct)
    {
        var jwt = CreateAppJwt();
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/repos/{owner}/{repo}/installation");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

        using var response = await _httpClient.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"Failed to resolve GitHub App installation for '{owner}/{repo}'. Status: {(int)response.StatusCode}");

        var json = await response.Content.ReadAsStringAsync(ct);
        using var document = JsonDocument.Parse(json);
        if (!document.RootElement.TryGetProperty("id", out var idProperty) || !idProperty.TryGetInt64(out var value))
        {
            _logger.LogError("Failed to resolve installation, got invalid response:\r\n{json}", json);
            throw new JsonException($"Failed to resolve GitHub App installation for '{owner}/{repo}'. Response was not containing valid 'id'.");
        }

        return value;
    }

    /// <summary>
    /// Creates a JWT signed with the GitHub App's private key using RS256.
    /// <br/>
    /// See <see href="https://docs.github.com/en/apps/creating-github-apps/authenticating-with-a-github-app/generating-a-json-web-token-jwt-for-a-github-app">
    /// Generating a JWT for a GitHub App
    /// </see>.
    /// </summary>
    private string CreateAppJwt()
    {
        var now = DateTimeOffset.UtcNow;
        var header = Base64UrlEncode(
            JsonSerializer.SerializeToUtf8Bytes(
                new GitHubJwtHeader { Algorithm = "RS256", TokenType = "JWT" },
                SourceGenerationContext.Default.GitHubJwtHeader
            )
        );

        var payload = Base64UrlEncode(
            JsonSerializer.SerializeToUtf8Bytes(
                new GitHubJwtPayload
                {
                    IssuedAt = now.ToUnixTimeSeconds(), 
                    Expiry = now.AddMinutes(9).ToUnixTimeSeconds(), 
                    Issuer = _config.AppId
                },
                SourceGenerationContext.Default.GitHubJwtPayload
            )
        );

        var signingInput = $"{header}.{payload}";
        var signature = SignWithRsa(signingInput);
        return $"{signingInput}.{signature}";
    }

    private string SignWithRsa(string input)
    {
        var data = Encoding.ASCII.GetBytes(input);
        var signature = _rsa.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        return Base64UrlEncode(signature);
    }

    // github is very... specific about it :slugwhat:
    private static string Base64UrlEncode(byte[] data)
    {
        return Convert.ToBase64String(data)
                      .TrimEnd('=')
                      .Replace('+', '-')
                      .Replace('/', '_');
    }

    /// <inheritdoc />
    public void InvalidateToken()
    {
        _installationToken = null;
        _installationTokenExpiry = null;
        _resolvedInstallationId = null;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _refreshLock.Dispose();
        _rsa.Dispose();
    }
}
