using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using SS14.Labeller.Helpers;

namespace SS14.Labeller.Tests;

[ExcludeFromCodeCoverage]
public partial class IntegrationTests
{
    private const string HookSecret = "asdasdasdasdasdasdasdadsadad";

    private CustomWebApplicationFactory _applicationFactory;

    private HttpClient _client;

    [SetUp]
    public void Setup()
    {
        Environment.SetEnvironmentVariable("GITHUB_WEBHOOK_SECRET", HookSecret);
        Environment.SetEnvironmentVariable("GITHUB_TOKEN", "DUMMY");


        _applicationFactory = new CustomWebApplicationFactory();
        _client = _applicationFactory.CreateClient();
    }

    [Test]
    public async Task Ping_NikIsStillCat()
    {
        var result = await _client.GetAsync("/");
        var content = await result.Content.ReadAsStringAsync();
        Assert.That(content.Contains("nik", StringComparison.InvariantCultureIgnoreCase));
        Assert.That(content.Contains("cat", StringComparison.InvariantCultureIgnoreCase));
    }

    private static async Task<StringContent> CreateRequestContent(string requestFileName, string eventType)
    {
        var filePath = Path.Combine("Resources", requestFileName);
        var text = await File.ReadAllTextAsync(filePath);
        var requestContent = new StringContent(text, Encoding.UTF8);

        var msgHash = EncodingHelper.ToHmacSha256(Encoding.UTF8.GetBytes(text), HookSecret);
        requestContent.Headers.Add("X-Hub-Signature-256", $"sha256={msgHash}");
        requestContent.Headers.Add("X-GitHub-Event", eventType);
        return requestContent;
    }

    [TearDown]
    public void TearDown()
    {
        _applicationFactory.Dispose();
    }
}