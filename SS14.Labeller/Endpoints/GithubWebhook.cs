using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using SS14.Labeller.Handlers;
using SS14.Labeller.Middlewares;
using SS14.Labeller.Models;

namespace SS14.Labeller.Endpoints;

public static class GithubWebhook
{
    public static void MapGithubWebhook(this WebApplication app)
    {
        app.Map("/webhook", builder =>
        {
            builder.UseHttpLogging();
            builder.UseRouting();
            builder.UseMiddleware<GitHubWebhookAuthorizationMiddleware>();
            builder.UseEndpoints(endpoints =>
            {
                endpoints.MapPost("/", HandleWebhook);
            });
        });
    }

    private static async Task<NoContent> HandleWebhook(
        EventBase @event,
        [FromServices] GitHubWebhookHandlerService handler,
        CancellationToken ct)
    {
        await handler.Handle(@event, ct);
        return TypedResults.NoContent();
    }
}