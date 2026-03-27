using Microsoft.Extensions.Options;
using SS14.Labeller.Configuration;
using SS14.Labeller.Database;
using SS14.Labeller.DiscourseApi;
using SS14.Labeller.GitHubApi;
using SS14.Labeller.Labelling;
using SS14.Labeller.Repository;
using System.Net.Http.Headers;
using Polly;
using Polly.Extensions.Http;
using SS14.Labeller.Initialization;
using MessagePipe;
using SS14.Labeller.Handlers;
using SS14.Labeller.Models;

namespace SS14.Labeller;

public static class Registry
{
    public static void RegisterDependencies(this IServiceCollection service, IConfiguration configuration)
    {
#pragma warning disable IL2026
        service.AddOptions<DiscourseConfig>()
               .Bind(configuration.GetSection(DiscourseConfig.Name))
               .ValidateDataAnnotations();

        service.AddOptions<GitHubConfig>()
               .Bind(configuration.GetSection(GitHubConfig.Name))
               .ValidateDataAnnotations();
#pragma warning restore IL2026

        service.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.TypeInfoResolverChain.Insert(0, SourceGenerationContext.Default);
        });

        service.AddHttpLogging(options =>
        {
            options.LoggingFields = Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.All;
        });
        service.AddHttpClient<IGitHubApiClient, GitHubApiClient>((sp, client) =>
        {
            var githubConfig = sp.GetRequiredService<IOptions<GitHubConfig>>().Value;

            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("SS14.Labeller", "1.0"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", githubConfig.Token);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        }).AddHttpMessageHandler<GithubRetryHandler>();
        service.AddTransient<GithubRetryHandler>();

        var discourseStartupConfig = new DiscourseConfig();
        configuration.Bind(DiscourseConfig.Name, discourseStartupConfig);

        if (discourseStartupConfig.Enable)
        {
            service.AddHttpClient<IDiscourseClient, DiscourseClient>((sp, client) =>
            {
                var discourseConfig = sp.GetRequiredService<IOptions<DiscourseConfig>>().Value;

                client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("SS14.Labeller", "1.0"));
                client.DefaultRequestHeaders.Add("Api-Key", discourseConfig.ApiKey);
                client.DefaultRequestHeaders.Add("Api-Username", discourseConfig.Username);
                client.BaseAddress = new Uri(discourseConfig.Url);
            }).SetHandlerLifetime(TimeSpan.FromMinutes(5))
            .AddPolicyHandler((sp, message) => GetDiscourseRetryPolicy(sp));
        }
        else
        {
            service.AddHttpClient<IDiscourseClient, DummyDiscourseClient>();
        }

        service.AddSingleton<ILabelManager, LabelManager>();


        service.AddSingleton<IDiscourseTopicsRepository, DiscourseTopicsRepository>();
        
        service.AddHostedService<ApplicationServicesInitializingBackgroundService>();

        service.AddSingleton<IOnApplicationStartInitializable, MessagePipeSubscriptionManager>();

        service.AddHostedService<DatabaseMigrationApplyingBackgroundService>();

        service.AddSingleton<IAsyncMessageHandler<IssuesEvent>, LabelIssueHandler>();
        service.AddSingleton<IAsyncMessageHandler<PullRequestEvent>, LabelPullRequestHandler>();
        service.AddSingleton<IAsyncMessageHandler<PullRequestReviewEvent>, LabelPullRequestReviewHandler>();

        service.AddMessagePipe(
            x => x.SetAutoRegistrationSearchAssemblies(typeof(Registry).Assembly)
        );

        service.AddSingleton<GenericPublisher>();
    }

    private static IAsyncPolicy<HttpResponseMessage> GetDiscourseRetryPolicy(IServiceProvider sp)
    {
        var options = sp.GetRequiredService<IOptionsMonitor<DiscourseConfig>>();
        return HttpPolicyExtensions
               .HandleTransientHttpError()
               .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.NotFound)
               .WaitAndRetryAsync(options.CurrentValue.RetryAttempts, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
    }
}