using System.Text.Json;
using System.Text;
using Microsoft.Extensions.Options;
using SS14.Labeller.Configuration;
using SS14.Labeller.Labelling.Labels;
using SS14.Labeller.Messages;
using SS14.Labeller.Models;

namespace SS14.Labeller.GitHubApi;

public class GitHubApiClient(HttpClient httpClient, IOptions<GitHubConfig> config) : IGitHubApiClient
{
    private const string BaseUrl = "https://api.github.com";

    private readonly GitHubConfig _config = config.Value;

    /// <inheritdoc />
    public async Task AddLabel(string owner, string repoName, int number, LabelBase label, CancellationToken ct)
    {
        EnsureCallingConfiguredRepo(owner, repoName);

        var request = new AddLabelRequest { labels = [label] };
        var json = JsonSerializer.Serialize(request, SourceGenerationContext.Default.AddLabelRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        await httpClient.PostAsync($"{BaseUrl}/repos/{owner}/{repoName}/issues/{number}/labels", content, ct);
    }

    public Task AddLabel(GithubRepo repo, int number, LabelBase label, CancellationToken ct)
    {
        return AddLabel(repo.Owner.Login, repo.Name, number, label, ct);
    }

    /// <inheritdoc />
    public async Task RemoveLabel(string owner, string repoName, int number, LabelBase label, CancellationToken ct)
    {
        EnsureCallingConfiguredRepo(owner, repoName);

        await httpClient.DeleteAsync($"{BaseUrl}/repos/{owner}/{repoName}/issues/{number}/labels/{Uri.EscapeDataString(label)}", ct);
    }

    public Task RemoveLabel(GithubRepo repo, int number, LabelBase label, CancellationToken ct)
    {
        return RemoveLabel(repo.Owner.Login, repo.Name, number, label, ct);
    }

    public async Task<List<string>> GetChangedFiles(GithubRepo repo, int prNumber, CancellationToken ct)
    {
        EnsureCallingConfiguredRepo(repo.Owner.Login, repo.Name);

        // TODO: Ratelimit? Might explode on big PRs???
        // TODO: Update to use ParseNextPageUrl

        var files = new List<string>();
        var page = 1;
        while (true)
        {
            var res = await httpClient.GetAsync($"{BaseUrl}/repos/{repo.Owner.Login}/{repo.Name}/pulls/{prNumber}/files?per_page=100&page={page}", ct);
            if (!res.IsSuccessStatusCode)
                break; // TODO: Logging?

            var content = await res.Content.ReadAsStringAsync(ct);
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
        EnsureCallingConfiguredRepo(repo.Owner.Login, repo.Name);

        var permRes = await httpClient.GetAsync($"{BaseUrl}/repos/{repo.Owner.Login}/{repo.Name}/collaborators/{user}/permission", ct);
        if (!permRes.IsSuccessStatusCode)
        {
            throw new Exception("Failed to get permissions! Does the github token have enough access?");
        }

        var permJson = JsonDocument.Parse(await permRes.Content.ReadAsStringAsync(ct));
        var requestedPermission = permJson.RootElement.GetProperty("permission").GetString();
        return requestedPermission is "write" or "admin";
    }

    public async Task AddComment(GithubRepo repo, int number, string comment, CancellationToken ct)
    {
        EnsureCallingConfiguredRepo(repo.Owner.Login, repo.Name);

        var request = new AddCommentRequest { body = $"{comment}\n\n{StatusMessages.CommentPostfix}" };
        var json = JsonSerializer.Serialize(request, SourceGenerationContext.Default.AddCommentRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        await httpClient.PostAsync($"{BaseUrl}/repos/{repo.Owner.Login}/{repo.Name}/issues/{number}/comments", content, ct);
    }

    public async Task<List<IssueComment>> GetComments(GithubRepo repo, int prNumber, CancellationToken ct)
    {
        EnsureCallingConfiguredRepo(repo.Owner.Login, repo.Name);

        var allComments = new List<IssueComment>();
        var url = $"{BaseUrl}/repos/{repo.Owner.Login}/{repo.Name}/issues/{prNumber}/comments?per_page=100";

        while (true)
        {
            var res = await httpClient.GetAsync(url, ct);
            if (!res.IsSuccessStatusCode)
                break; // TODO: Logging?

            var json = await res.Content.ReadAsStringAsync(ct);
            var comments = (IssueComment[])JsonSerializer.Deserialize(json, typeof(IssueComment[]), SourceGenerationContext.DeserializationContext)!;
            allComments.AddRange(comments);

            if (res.Headers.TryGetValues("Link", out var linkHeaders))
            {
                var links = linkHeaders.FirstOrDefault();
                url = ParseNextPageUrl(links);
            }
            else
                break;
        }


        return allComments;
    }

    private void EnsureCallingConfiguredRepo(string owner, string repo)
    {
        if (!string.Equals(owner, _config.Owner)
            || !string.Equals(repo, _config.Repo))
        {
            throw new InvalidOperationException(
                $"GitHub API request to '{owner}/{repo}' does not match the configured repository: " +
                $"'{_config.Owner}/{_config.Repo}'. Requests to other repositories would not be authenticated.");
        }
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