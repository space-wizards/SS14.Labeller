using System;
using System.Net.Http;
using System.Threading.Tasks;
using NUnit.Framework;

namespace SS14.Labeller.Tests;

public class IntegrationTests
{
    private CustomWebApplicationFactory _applicationFactory;

    private HttpClient _client;

    [SetUp]
    public void Setup()
    {
        _applicationFactory = new CustomWebApplicationFactory();
        _client = _applicationFactory.CreateClient();
    }

    [Test]
    public async Task Ping()
    {
        var result = await _client.GetAsync("/");
        var content = await result.Content.ReadAsStringAsync();
        Assert.That(content.Contains("nik", StringComparison.InvariantCultureIgnoreCase));
        Assert.That(content.Contains("cat", StringComparison.InvariantCultureIgnoreCase));
    }

    [TearDown]
    public void TearDown()
    {
        _applicationFactory.Dispose();
    }
}