using System.Net.Http.Headers;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SS14.Labeller.Configuration;
using SS14.Labeller.Database;
using SS14.Labeller.DiscourseApi;
using SS14.Labeller.GitHubApi;
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
        builder.Configuration.AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", true, true);

        builder.Services.AddOptions<DiscourseConfig>()
               .Bind(builder.Configuration.GetSection(DiscourseConfig.Name))
               .ValidateDataAnnotations();

        builder.Services.AddOptions<GitHubConfig>()
               .Bind(builder.Configuration.GetSection(GitHubConfig.Name))
               .ValidateDataAnnotations();

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
        builder.Services.AddHttpClient<IGitHubApiClient, GitHubApiClient>((sp, client) =>
        {
            var githubConfig = sp.GetRequiredService<IOptions<GitHubConfig>>().Value;

            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("SS14.Labeller", "1.0"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", githubConfig.Token);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        });
        builder.Services.AddHttpClient<IDiscourseClient, DiscourseClient>((sp, client) =>
        {
            var discourseConfig = sp.GetRequiredService<IOptions<DiscourseConfig>>().Value;

            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("SS14.Labeller", "1.0"));
            client.DefaultRequestHeaders.Add("Api-Key", discourseConfig.ApiKey);
            client.DefaultRequestHeaders.Add("Api-Username", discourseConfig.Username);
            client.BaseAddress = new Uri(discourseConfig.Url);
        });

        builder.Services.AddSingleton<RequestHandlerBase, LabelIssueHandler>();
        builder.Services.AddSingleton<RequestHandlerBase, LabelPullRequestReviewHandler>();
        builder.Services.AddSingleton<RequestHandlerBase, LabelPullRequestHandler>();

        builder.Services.AddSingleton<DataManager>();
        builder.Services.AddHostedService(p => p.GetRequiredService<DataManager>());

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