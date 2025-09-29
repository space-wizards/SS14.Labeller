using Microsoft.Extensions.Options;
using SS14.Labeller.Configuration;
using SS14.Labeller.Helpers;

namespace SS14.Labeller.Middlewares;

public class GitHubWebhookAuthorizationMiddleware(RequestDelegate next, IOptions<GitHubConfig> options)
{
    public async Task InvokeAsync(HttpContext context)
    { 
        // Enable buffering of the request body
        context.Request.EnableBuffering();

        using var memStream = new MemoryStream();
        await context.Request.Body.CopyToAsync(memStream);
        var bodyBytes = memStream.ToArray();

        var headers = context.Request.Headers;
        if (!SecurityHelper.IsRequestAuthorized(bodyBytes, options.Value.WebhookSecret, headers, out var errorResponse))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsync($"Forbidden: {errorResponse}.");
            return;
        }
        context.Request.Body.Seek(0, SeekOrigin.Begin);
        await next(context);
    }
}