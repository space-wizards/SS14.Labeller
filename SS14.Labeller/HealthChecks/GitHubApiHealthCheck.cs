using Microsoft.Extensions.Diagnostics.HealthChecks;
using SS14.Labeller.GitHubApi;

namespace SS14.Labeller.HealthChecks;


public class GitHubApiKeyHealthCheck(IHttpClientFactory httpClientFactory) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var client = httpClientFactory.CreateClient(nameof(IGitHubApiClient));

            // Make a simple, low-impact request to verify the key
            var response = await client.GetAsync("https://api.github.com/user", cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return HealthCheckResult.Healthy("GitHub API key is valid.");
            }

            if (response.StatusCode is System.Net.HttpStatusCode.Unauthorized or System.Net.HttpStatusCode.Forbidden)
            {
                return HealthCheckResult.Unhealthy("GitHub API key is invalid or lacks necessary permissions.");
            }

            return HealthCheckResult.Degraded($"GitHub API returned an unexpected status code: {response.StatusCode}");
        }
        catch (HttpRequestException ex)
        {
            return HealthCheckResult.Unhealthy($"Failed to connect to GitHub API: {ex.Message}");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy($"An error occurred during GitHub API key health check: {ex.Message}");
        }
    }
}