using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc;
using SS14.Labeller.GitHubApi;
using SS14.Labeller.Handlers;
using SS14.Labeller.Helpers;

namespace SS14.Labeller;

public class Program
{
    public static void Main(string[] args)
    {
        var githubSecret = Environment.GetEnvironmentVariable("GITHUB_WEBHOOK_SECRET");
        var githubToken = Environment.GetEnvironmentVariable("GITHUB_TOKEN");

        if (githubSecret == null || githubToken == null)
        {
            throw new InvalidOperationException("Missing required GITHUB_SECRET and GITHUB_TOKEN in ENV.");
        }

        var builder = WebApplication.CreateSlimBuilder(args);

        builder.Services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.TypeInfoResolverChain.Insert(0, SourceGenerationContext.Default);
        });

        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();

        builder.Services.AddHttpLogging(options =>
        {
            options.LoggingFields = Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.All;
        });
        builder.Services.AddHttpClient<IGitHubApiClient, GitHubApiClient>(client =>
        {
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("SS14.Labeller", "1.0"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", githubToken);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        });

        builder.Services.AddSingleton<RequestHandlerBase, LabelIssueHandler>();
        builder.Services.AddSingleton<RequestHandlerBase, LabelPullRequestReviewHandler>();
        builder.Services.AddSingleton<RequestHandlerBase, LabelPullRequestHandler>();

        builder.Services.AddSingleton<IReadOnlyDictionary<string, RequestHandlerBase>>(
            sp => sp.GetServices<RequestHandlerBase>()
                    .ToDictionary(x => x.EventType)
        );

        var app = builder.Build();

        app.UseHttpLogging();

        app.MapGet("/", () => Results.Ok("Nik is a cat!"));

        app.MapPost(
            "/webhook", 
            async (
                HttpContext context, 
                [FromHeader(Name = "X-GitHub-Event")] string githubEvent,
                [FromServices] IReadOnlyDictionary<string, RequestHandlerBase> handlers,
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
                if (!SecurityHelper.IsRequestAuthorized(bodyBytes, githubSecret, headers, out var errorResponse)) 
                    return errorResponse;

                if (handlers.TryGetValue(githubEvent, out var handler))
                {
                    await handler.Handle(bodyBytes, context.RequestAborted);
                }
                else
                {
                    logger.LogWarning(
                        "Unexpected 'X-GitHub-Event' header, cannot handle event of type '{eventType}'.", 
                        githubEvent
                    );
                }

                return Results.NoContent();
            });

        app.Run();
    }
}