using Microsoft.Extensions.Options;
using SS14.Labeller.Configuration;
using SS14.Labeller.Database;
using SS14.Labeller.DiscourseApi;
using SS14.Labeller.GitHubApi;
using SS14.Labeller.Handlers;
using SS14.Labeller.Labelling;
using SS14.Labeller.Repository;
using System.Net.Http.Headers;

namespace SS14.Labeller;

public static class Registry
{
    public static void RegisterDependencies(this IServiceCollection service, IConfiguration configuration)
    {
        service.AddOptions<DiscourseConfig>()
                         .Bind(configuration.GetSection(DiscourseConfig.Name))
                         .ValidateDataAnnotations();

        service.AddOptions<GitHubConfig>()
                         .Bind(configuration.GetSection(GitHubConfig.Name))
                         .ValidateDataAnnotations();

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
        });

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
            });
        }
        else
        {
            service.AddHttpClient<IDiscourseClient, DummyDiscourseClient>();
        }

        service.AddSingleton<ILabelManager, LabelManager>();
        
        service.AddSingleton<RequestHandlerBase, LabelIssueHandler>();
        service.AddSingleton<RequestHandlerBase, LabelPullRequestReviewHandler>();
        service.AddSingleton<RequestHandlerBase, LabelPullRequestHandler>();

        service.AddSingleton<IDiscourseTopicsRepository, DiscourseTopicsRepository>();

        service.AddHostedService<DatabaseMigrationApplyingBackgroundService>();

        service.AddSingleton<IReadOnlyDictionary<Type, RequestHandlerBase>>(
            sp => sp.GetServices<RequestHandlerBase>()
                    .ToDictionary(x => x.CanHandleType)
        );
    }
}