using System.Text.Json;
using System.Text;
using SS14.Labeller.Models;

namespace SS14.Labeller.GitHubApi;

public class GitHubApiClient(HttpClient httpClient) : IGitHubApiClient
{
    private const string BaseUrl = "https://api.github.com";

    public async Task AddLabel(Repository repo, int number, string label)
    {
        var request = new AddLabelRequest { labels = [label] };
        var json = JsonSerializer.Serialize(request, SourceGenerationContext.Default.AddLabelRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        await httpClient.PostAsync($"{BaseUrl}/repos/{repo.Owner.Login}/{repo.Name}/issues/{number}/labels", content);
    }

    public async Task RemoveLabel(Repository repo, int number, string label)
    {
        await httpClient.DeleteAsync($"{BaseUrl}/repos/{repo.Owner.Login}/{repo.Name}/issues/{number}/labels/{Uri.EscapeDataString(label)}");
    }

    public async Task<List<string>> GetChangedFiles(Repository repo, int prNumber)
    {
        // TODO: Ratelimit? Might explode on big PRs???

        var files = new List<string>();
        var page = 1;
        while (true)
        {
            var res = await httpClient.GetAsync($"{BaseUrl}/repos/{repo.Owner.Login}/{repo.Name}/pulls/{prNumber}/files?per_page=100&page={page}");
            if (!res.IsSuccessStatusCode)
                break; // TODO: Logging?

            var content = await res.Content.ReadAsStringAsync();
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
    public async Task<string?> GetPermission(Repository repo, string? user)
    {
        var permRes = await httpClient.GetAsync($"{BaseUrl}/repos/{repo.Owner.Login}/{repo.Name}/collaborators/{user}/permission");
        if (!permRes.IsSuccessStatusCode)
        {
            throw new Exception("Failed to get permissions! Does the github token have enough access?");
        }
        var permJson = JsonDocument.Parse(await permRes.Content.ReadAsStringAsync());
        return permJson.RootElement.GetProperty("permission").GetString();
    }
}

public class AddLabelRequest
{
    // ReSharper disable once InconsistentNaming
    public string[] labels { get; set; } = [];
}