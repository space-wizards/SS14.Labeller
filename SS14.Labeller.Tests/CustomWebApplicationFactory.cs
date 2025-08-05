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

namespace SS14.Labeller.Tests;

[ExcludeFromCodeCoverage]
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    public IGitHubApiClient GitHubApiClient { get; private set; }
    public IDiscourseClient DiscourseClient { get; private set; }

    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        GitHubApiClient = Substitute.For<IGitHubApiClient>();
        DiscourseClient = Substitute.For<IDiscourseClient>();

        base.ConfigureWebHost(builder);
        builder.ConfigureServices(sp =>
        {
            sp.Replace(new ServiceDescriptor(typeof(IGitHubApiClient), GitHubApiClient));
            sp.Replace(new ServiceDescriptor(typeof(IDiscourseClient), DiscourseClient));
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