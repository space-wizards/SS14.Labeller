﻿using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http.Headers;
using Microsoft.Extensions.Options;
using SS14.Labeller.Configuration;

namespace SS14.Labeller.GitHubApi;

/// <summary>
/// Basic rate limiter for the GitHub api! Will ensure there is only ever one outgoing request at a time and all
/// requests respect the rate limit the best they can.
/// <br/>
/// <br/> Links to the api for more information:
/// <br/> <see href="https://docs.github.com/en/rest/using-the-rest-api/best-practices-for-using-the-rest-api?apiVersion=2022-11-28">Best practices</see>
/// <br/> <see href="https://docs.github.com/en/rest/using-the-rest-api/rate-limits-for-the-rest-api?apiVersion=2022-11-28">Rate limit information</see>
/// </summary>
/// <remarks> This was designed for the 2022-11-28 version of the API. </remarks>
public sealed class GithubRetryHandler(HttpMessageHandler innerHandler, IOptionsMonitor<GitHubConfig> githubConfig, ILogger<GithubRetryHandler> logger) : DelegatingHandler(innerHandler)
{
    private const int MaxWaitSeconds = 32;

    /// Extra buffer time (In seconds) after getting rate limited we don't make the request exactly when we get more credits.
    private static readonly TimeSpan ExtraBufferTime = TimeSpan.FromSeconds(1);

    #region Headers

    private const string RetryAfterHeader = "retry-after";

    private const string RemainingHeader = "x-ratelimit-remaining";
    private const string RateLimitResetHeader = "x-ratelimit-reset";

    #endregion

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        HttpResponseMessage? response = null;
        var i = 0;
        var maxRetry = githubConfig.CurrentValue.MaxRetryAttempt;
        do
        {
            try
            {
                i++;
                response = await base.SendAsync(request, cancellationToken);
            }
            catch (HttpRequestException)
            {
                if (i < maxRetry)
                {
                    var waitTime = ExponentialBackoff(i);
                    await Task.Delay(waitTime, cancellationToken);

                    continue;
                }

                throw;
            }
            if (response.IsSuccessStatusCode)
                return response;

            if (i < maxRetry)
            {
                var waitTime = CalculateNextRequestTime(response, i);
                await Task.Delay(waitTime, cancellationToken);
            }
        } while (response?.IsSuccessStatusCode != true && i < maxRetry);

        return response!;
    }

    /// <summary>
    /// Follows these guidelines but also has a small buffer so you should never quite hit zero:
    /// <br/>
    /// <see href="https://docs.github.com/en/rest/using-the-rest-api/best-practices-for-using-the-rest-api?apiVersion=2022-11-28#handle-rate-limit-errors-appropriately"/>
    /// </summary>
    /// <param name="response">The last response from the API.</param>
    /// <param name="attempt">Number of current call attempt.</param>
    /// <returns>The amount of time to wait until the next request.</returns>
    private TimeSpan CalculateNextRequestTime(HttpResponseMessage response, int attempt)
    {
        var headers = response.Headers;
        var statusCode = response.StatusCode;

        // Specific checks for rate limits.
        if (statusCode is HttpStatusCode.Forbidden or HttpStatusCode.TooManyRequests)
        {
            // Retry after header
            if (TryGetHeaderAsLong(headers, RetryAfterHeader, out var retryAfterSeconds))
                return TimeSpan.FromSeconds(retryAfterSeconds.Value) + ExtraBufferTime;

            // Reset header (Tells us when we get more api credits)
            if (TryGetHeaderAsLong(headers, RemainingHeader, out var remainingRequests)
                && TryGetHeaderAsLong(headers, RateLimitResetHeader, out var resetTime)
                && remainingRequests == 0)
            {
                var delayTime = resetTime.Value - DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                logger.LogWarning(
                    "github returned '{status}' status, have to wait until limit reset - in '{delay}' seconds",
                    response.StatusCode,
                    delayTime
                );
                return TimeSpan.FromSeconds(delayTime) + ExtraBufferTime;
            }
        }

        // If the status code is not the expected one or the rate limit checks are failing, just do an exponential backoff.
        return ExponentialBackoff(attempt);
    }

    private static TimeSpan ExponentialBackoff(int i)
    {
        return TimeSpan.FromSeconds(Math.Min(MaxWaitSeconds, Math.Pow(2, i)));
    }

    /// <summary>
    /// A simple helper function that just tries to parse a header value that is expected to be a long int.
    /// In general, there are just a lot of single value headers that are longs so this removes a lot of duplicate code.
    /// </summary>
    /// <param name="headers">The headers that you want to search.</param>
    /// <param name="header">The header you want to get the long value for.</param>
    /// <param name="value">Value of header, if found, null otherwise.</param>
    /// <returns>The headers value if it exists, null otherwise.</returns>
    public static bool TryGetHeaderAsLong(HttpResponseHeaders? headers, string header, [NotNullWhen(true)] out long? value)
    {
        value = null;
        if (headers == null)
            return false;

        if (!headers.TryGetValues(header, out var headerValues))
            return false;

        if (!long.TryParse(headerValues.First(), out var result))
            return false;

        value = result;
        return true;
    }
}