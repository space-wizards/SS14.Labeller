using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace SS14.Labeller.Tests.GitHubApi.Mocks;

[ExcludeFromCodeCoverage]
public class MockInnerHandler : HttpMessageHandler
{
    public Queue<HttpResponseMessage> Responses { get; } = new();
    public List<HttpRequestMessage> Requests { get; } = new();
    public List<string> AuthTokens { get; } = new();
    public HttpRequestMessage? LastRequest { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(request);
        AuthTokens.Add(request.Headers.Authorization?.Parameter ?? string.Empty);
        LastRequest = request;
        return Send(request, cancellationToken);
    }

    public virtual Task<HttpResponseMessage> Send(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = Responses.Count > 0
            ? Responses.Dequeue()
            : new HttpResponseMessage(HttpStatusCode.OK);
        return Task.FromResult(response);
    }
}