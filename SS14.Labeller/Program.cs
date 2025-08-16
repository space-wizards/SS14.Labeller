using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SS14.Labeller.Configuration;
using SS14.Labeller.Handlers;
using SS14.Labeller.Helpers;

[module:DapperAot]

namespace SS14.Labeller;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateSlimBuilder(args);

        builder.Configuration.AddJsonFile("appsettings.json", true, true);
        builder.Configuration.AddJsonFile("appsettings.Secret.json", true, true);
        builder.Configuration.AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", true, true);

        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();

        builder.Services.RegisterDependencies(builder.Configuration);

        var app = builder.Build();

        app.UseHttpLogging();

        app.MapGet("/", () => Results.Ok("Nik is a cat!"));

        app.MapPost(
            "/webhook",
            async (
                HttpContext context,
                [FromHeader(Name = "X-GitHub-Event")] string githubEvent,
                [FromServices] IReadOnlyDictionary<string, RequestHandlerBase> handlers,
                [FromServices] IOptions<GitHubConfig> githubConfig,
                [FromServices] ILogger<Program> logger
            ) =>
            {
                if (string.IsNullOrEmpty(githubEvent))
                    return Results.BadRequest("Missing GitHub event.");

                var request = context.Request;

                using var memStream = new MemoryStream();
                await request.Body.CopyToAsync(memStream);
                var bodyBytes = memStream.ToArray();

                var headers = request.Headers;
                if (!SecurityHelper.IsRequestAuthorized(bodyBytes, githubConfig.Value.WebhookSecret, headers, out var errorResponse))
                    return errorResponse;

                if (handlers.TryGetValue(githubEvent, out var handler))
                {
                    await handler.Handle(bodyBytes, context.RequestAborted);
                }

                return Results.NoContent();
            });

        app.Run();
    }
}