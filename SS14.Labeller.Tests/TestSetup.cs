using System.IO;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;

namespace SS14.Labeller.Tests;

[SetUpFixture]
public class TestSetup
{
    public static IConfiguration Configuration { get; private set; }

    [OneTimeSetUp]
    public void RunBeforeAnyTests()
    {
        Configuration = new ConfigurationBuilder()
                        .SetBasePath(Directory.GetCurrentDirectory())
                        .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                        .AddEnvironmentVariables()
                        .Build();
    }
}