using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using SS14.Labeller.GitHubApi;

namespace SS14.Labeller.Tests;

[ExcludeFromCodeCoverage]
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    public IGitHubApiClient GitHubApiClient { get; private set; }

    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        GitHubApiClient = Substitute.For<IGitHubApiClient>();

        base.ConfigureWebHost(builder);
        builder.ConfigureServices(sp =>
        {
            sp.Replace(new ServiceDescriptor(typeof(IGitHubApiClient), GitHubApiClient));
        });
    }
}