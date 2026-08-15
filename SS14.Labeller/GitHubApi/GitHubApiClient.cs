using System.Text.Json;
using System.Text;
using SS14.Labeller.Messages;
using SS14.Labeller.Models;
using SS14.Labeller.Labelling.Labels;

namespace SS14.Labeller.GitHubApi;

public class GitHubApiClient(HttpClient httpClient, ILogger<GitHubApiClient> logger) : IGitHubApiClient
{
    private const string BaseUrl = "https://api.github.com";

    /// <inheritdoc />
    public async Task AddLabel(string owner, string repoName, int number, LabelBase label, CancellationToken ct)
    {
        var request = new AddLabelRequest { labels = [label] };
        var json = JsonSerializer.Serialize(request, SourceGenerationContext.Default.AddLabelRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        await SendAndLogErrorsAsync(
            () => httpClient.PostAsync(IssueUrl(owner, repoName, number, "labels"), content, ct),
            "add label",
            ct);
    }

    public Task AddLabel(GithubRepo repo, int number, LabelBase label, CancellationToken ct)
    {
        return AddLabel(repo.Owner.Login, repo.Name, number, label, ct);
    }

    /// <inheritdoc />
    public async Task RemoveLabel(string owner, string repoName, int number, LabelBase label, CancellationToken ct)
    {
        await SendAndLogErrorsAsync(
            () => httpClient.DeleteAsync(IssueUrl(owner, repoName, number, $"labels/{Uri.EscapeDataString(label)}"), ct),
            "remove label",
            ct);
    }

    public Task RemoveLabel(GithubRepo repo, int number, LabelBase label, CancellationToken ct)
    {
        return RemoveLabel(repo.Owner.Login, repo.Name, number, label, ct);
    }

    public async Task<List<string>> GetChangedFiles(GithubRepo repo, int prNumber, CancellationToken ct)
    {
        // TODO: Ratelimit? Might explode on big PRs???
        // TODO: Update to use ParseNextPageUrl

        var files = new List<string>();
        var page = 1;
        while (true)
        {
            var url = RepoUrl(repo.Owner.Login, repo.Name, $"pulls/{prNumber}/files?per_page=100&page={page}");
            var response = await SendAndLogErrorsAsync(() => httpClient.GetAsync(url, ct), "get changed files", ct);
            if (!response.IsSuccessStatusCode)
                break;

            var content = await response.Content.ReadAsStringAsync(ct);
            var json = JsonDocument.Parse(content);
            var batch = json.RootElement.EnumerateArray().Select(f => f.GetProperty("filename").GetString()!).ToList();
            if (batch.Count == 0) break;

            files.AddRange(batch);
            if (batch.Count < 100) break;

            page++;
        }

        return files;
    }

    /// <inheritdoc />
    public async Task<bool> IsMaintainer(string? user, GithubRepo repo, CancellationToken ct)
    {
        var url = RepoUrl(repo.Owner.Login, repo.Name, $"collaborators/{user}/permission");
        var response = await SendAndLogErrorsAsync(() => httpClient.GetAsync(url, ct), "IsMaintainer check", ct);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException("Failed to get permissions! Does the github token have enough access?");
        }

        var permJson = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
        var requestedPermission = permJson.RootElement.GetProperty("permission").GetString();
        return requestedPermission is "write" or "admin";
    }

    public async Task AddComment(GithubRepo repo, int number, string comment, CancellationToken ct)
    {
        var request = new AddCommentRequest { body = $"{comment}\n\n{StatusMessages.CommentPostfix}" };
        var json = JsonSerializer.Serialize(request, SourceGenerationContext.Default.AddCommentRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        await SendAndLogErrorsAsync(
            () => httpClient.PostAsync(IssueUrl(repo.Owner.Login, repo.Name, number, "update comments"), content, ct),
            "AddComment",
            ct);
    }

    public async Task<List<IssueComment>> GetComments(GithubRepo repo, int prNumber, CancellationToken ct)
    {
        var allComments = new List<IssueComment>();
        var url = $"{BaseUrl}/repos/{repo.Owner.Login}/{repo.Name}/issues/{prNumber}/comments?per_page=100";

        while (true)
        {
            var response = await SendAndLogErrorsAsync(() => httpClient.GetAsync(url, ct), "get comments", ct);
            if (!response.IsSuccessStatusCode)
                break;

            var json = await response.Content.ReadAsStringAsync(ct);
            var comments = (IssueComment[])JsonSerializer.Deserialize(json, typeof(IssueComment[]), SourceGenerationContext.DeserializationContext)!;
            allComments.AddRange(comments);

            if (response.Headers.TryGetValues("Link", out var linkHeaders))
            {
                var links = linkHeaders.FirstOrDefault();
                url = ParseNextPageUrl(links);
            }
            else
                break;
        }


        return allComments;
    }

    private async Task<HttpResponseMessage> SendAndLogErrorsAsync(
        Func<Task<HttpResponseMessage>> send,
        string operation,
        CancellationToken ct)
    {
        var response = await send();

        if (response.IsSuccessStatusCode)
            return response;

        var body = await response.Content.ReadAsStringAsync(ct);
        logger.LogError(
            "GitHub API request '{Operation}' failed with status {StatusCode}: {Body}",
            operation,
            (int)response.StatusCode,
            body);

        return response;
    }

    private static string RepoUrl(string owner, string repoName, string path)
    {
        return $"{BaseUrl}/repos/{owner}/{repoName}/{path}";
    }

    private static string IssueUrl(string owner, string repoName, int number, string subPath)
    {
        return RepoUrl(owner, repoName, $"issues/{number}/{subPath}");
    }

    private static string? ParseNextPageUrl(string? linkHeader)
    {
        if (string.IsNullOrEmpty(linkHeader))
            return null;

        var links = linkHeader.Split(',');
        // ReSharper disable once LoopCanBeConvertedToQuery - no.
        foreach (var link in links)
        {
            var parts = link.Split(';');
            if (parts.Length < 2)
                continue;

            var urlPart = parts[0].Trim().Trim('<', '>');
            var relPart = parts[1].Trim();

            if (relPart.Equals("rel=\"next\"", StringComparison.OrdinalIgnoreCase))
                return urlPart;
        }

        return null;
    }
}

public class AddLabelRequest
{
    // ReSharper disable once InconsistentNaming
    public string[] labels { get; set; } = [];
}

public class AddCommentRequest
{
    // ReSharper disable once InconsistentNaming
    public string body { get; set; } = string.Empty;
}