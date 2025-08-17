using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using SS14.Labeller.Database;
using SS14.Labeller.DiscourseApi;
using SS14.Labeller.GitHubApi;
using SS14.Labeller.Repository;

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
            var hostedServiceDescriptor = sp.First(d =>
                d.ServiceType == typeof(IHostedService) &&
                d.ImplementationType == typeof(DatabaseMigrationApplyingBackgroundService)); // Replace YourHostedService with the actual type

            sp.Remove(hostedServiceDescriptor);
        }).ConfigureAppConfiguration((context, configurationBuilder) =>
        {
            configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Discourse:ApiKey", "wawa" },
                { "Discourse:Username", "aw" },
                { "Discourse:DiscussionCategoryId", "42" },
                { "Discourse:Url", "http://wa.wa" },
                { "GitHub:WebhookSecret", IntegrationTests.HookSecret },
                { "GitHub:Token", "test-test" },
            });
        });
    }
}