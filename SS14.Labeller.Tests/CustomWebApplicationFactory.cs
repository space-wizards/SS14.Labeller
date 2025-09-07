using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using SS14.Labeller.DiscourseApi;
using SS14.Labeller.GitHubApi;
using SS14.Labeller.Repository;
using SS14.Labeller.Tests.IntegrationTests;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

namespace SS14.Labeller.Tests;

[ExcludeFromCodeCoverage]
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    public IGitHubApiClient GitHubApiClient { get; private set; }
    public IDiscourseClient DiscourseClient { get; private set; }
    public IDiscourseTopicsRepository TopicsRepository { get; private set; }

    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        GitHubApiClient = Substitute.For<IGitHubApiClient>();
        DiscourseClient = Substitute.For<IDiscourseClient>();
        TopicsRepository = Substitute.For<IDiscourseTopicsRepository>();

        base.ConfigureWebHost(builder);
        builder.ConfigureServices(sp =>
        {
            sp.Replace(new ServiceDescriptor(typeof(IGitHubApiClient), GitHubApiClient));
            sp.Replace(new ServiceDescriptor(typeof(IDiscourseClient), DiscourseClient));
            sp.Replace(new ServiceDescriptor(typeof(IDiscourseTopicsRepository), TopicsRepository));
        }).ConfigureAppConfiguration((context, configurationBuilder) =>
        {
            configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Discourse:ApiKey", "wawa" },
                { "Discourse:Username", "aw" },
                { "Discourse:DiscussionCategoryId", "42" },
                { "Discourse:Url", "http://wa.wa" },
                { "GitHub:WebhookSecret", HandlersTests.HookSecret },
                { "GitHub:Token", "test-test" },
            });
        });
    }
}