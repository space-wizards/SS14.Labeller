using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.FileSystemGlobbing;

const string githubApiBase = "https://api.github.com";

const string labelStatusPrefix = "S: ";
const string labelStatusUntriaged = labelStatusPrefix + "Untriaged";
const string labelStatusRequireReview = labelStatusPrefix + "Needs Review"; // no idea why its called this
const string labelStatusAwaitingChanges = labelStatusPrefix + "Awaiting Changes";
const string labelStatusApproved = labelStatusPrefix + "Approved";

const string labelBranchPrefix = "Branch: ";
const string labelBranchStable = labelBranchPrefix + "Stable";
const string labelBranchStaging = labelBranchPrefix + "Staging";

const string labelChangesPrefix = "Changes: ";
const string labelChangesAudio = labelChangesPrefix + "Audio";
const string labelChangesMap = labelChangesPrefix + "Map";
const string labelChangesNoCSharp = labelChangesPrefix + "No C#";
const string labelChangesShaders = labelChangesPrefix + "Shaders";
const string labelChangesSprites = labelChangesPrefix + "Sprites";
const string labelChangesUi = labelChangesPrefix + "UI";

var builder = WebApplication.CreateSlimBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, GitHubJsonContext.Default);
});

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Services.AddHttpLogging(options =>
{
    options.LoggingFields = Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.All;
});
builder.Services.AddHttpClient();

var app = builder.Build();

app.UseHttpLogging();

var githubSecret = Environment.GetEnvironmentVariable("GITHUB_WEBHOOK_SECRET");
var githubToken = Environment.GetEnvironmentVariable("GITHUB_TOKEN");

if (githubSecret == null || githubToken == null)
{
    throw new InvalidOperationException("Missing required GITHUB_SECRET and GITHUB_TOKEN in ENV.");
}

app.MapGet("/", () => Results.Ok("Nik is a cat!"));

app.MapPost("/webhook", async (HttpContext context, IHttpClientFactory clientFactory) =>
{
    using var memStream = new MemoryStream();
    await context.Request.Body.CopyToAsync(memStream);
    var bodyBytes = memStream.ToArray();
    var bodyString = Encoding.UTF8.GetString(bodyBytes);

    if (!context.Request.Headers.TryGetValue("X-Hub-Signature-256", out var signatureHeader))
        return Results.BadRequest("Missing signature header.");

    var expectedSignature = "sha256=" + ToHmacSha256(bodyBytes, githubSecret);
    if (!CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(expectedSignature), Encoding.UTF8.GetBytes(signatureHeader!)))
        return Results.Unauthorized();

    var githubEvent = context.Request.Headers["X-GitHub-Event"].FirstOrDefault();
    if (string.IsNullOrEmpty(githubEvent))
        return Results.BadRequest("Missing GitHub event.");

    var json = JsonDocument.Parse(bodyString);
    var client = clientFactory.CreateClient();
    client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("SS14.Labeller", "1.0"));
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", githubToken);
    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));

    switch (githubEvent)
    {
        case "pull_request":
            await PrHandler(json, client);
            break;

        case "issues":
            await IssuesHandler(json, client);
            break;

        case "pull_request_review":
            await PrReviewHandler(json, client);
            break;
    }

    return Results.Ok();
});

app.Run();
return;

async Task PrReviewHandler(JsonDocument json, HttpClient client)
{
    var review = json.RootElement.GetProperty("review");
    var pr = json.RootElement.GetProperty("pull_request");
    var repo = json.RootElement.GetProperty("repository");
    var user = review.GetProperty("user").GetProperty("login").GetString();
    var state = review.GetProperty("state").GetString();

    // only process if the review state is "approved" or "changes_requested" (ignore comments and other states)
    if (state != "approved" && state != "changes_requested")
        return;

    // Ignore reviews if PR is closed or merged
    var prState = pr.GetProperty("state").GetString();
    // "closed" means closed or merged, but let's also check for merged explicitly if available
    bool isClosed = prState == "closed";
    bool isMerged = pr.TryGetProperty("merged_at", out var mergedAtProp) && mergedAtProp.ValueKind != JsonValueKind.Null;
    if (isClosed || isMerged)
        return;

    var owner = repo.GetProperty("owner").GetProperty("login").GetString()!;
    var repoName = repo.GetProperty("name").GetString()!;
    var number = pr.GetProperty("number").GetInt32();

    var permRes = await client.GetAsync($"{githubApiBase}/repos/{owner}/{repoName}/collaborators/{user}/permission");
    if (!permRes.IsSuccessStatusCode)
    {
        throw new Exception("Failed to get permissions! Does the github token have enough access?");
    }

    var permJson = JsonDocument.Parse(await permRes.Content.ReadAsStringAsync());
    var permission = permJson.RootElement.GetProperty("permission").GetString();
    if (permission is "write" or "admin")
    {
        await RemoveLabel(client, owner, repoName, number, labelStatusRequireReview);

        if (state == "approved")
        {
            await AddLabel(client, owner, repoName, number, labelStatusApproved);
        }
        else if (state == "changes_requested")
        {
            await AddLabel(client, owner, repoName, number, labelStatusAwaitingChanges);
        }
    }
}

async Task IssuesHandler(JsonDocument json, HttpClient client)
{
    var action = json.RootElement.GetProperty("action").GetString();
    if (action == "opened")
    {
        var issue = json.RootElement.GetProperty("issue");
        var repo = json.RootElement.GetProperty("repository");
        var owner = repo.GetProperty("owner").GetProperty("login").GetString()!;
        var repoName = repo.GetProperty("name").GetString()!;
        var number = issue.GetProperty("number").GetInt32();
        var labels = issue.GetProperty("labels").EnumerateArray().Select(l => l.GetProperty("name").GetString()).ToList();

        if (labels.Count == 0)
            await AddLabel(client, owner, repoName, number, labelStatusUntriaged);
    }
}

async Task PrHandler(JsonDocument json, HttpClient client)
{
    // I null-supress the shit out of these because i assume the github webhook json will basically never update and will always return valid data

    var action = json.RootElement.GetProperty("action").GetString();
    var pr = json.RootElement.GetProperty("pull_request");
    var repo = json.RootElement.GetProperty("repository");
    var owner = repo.GetProperty("owner").GetProperty("login").GetString()!;
    var repoName = repo.GetProperty("name").GetString()!;
    var number = pr.GetProperty("number").GetInt32();
    var labels = pr.GetProperty("labels").EnumerateArray().Select(l => l.GetProperty("name").GetString()).ToList();
    var targetBranch = pr.GetProperty("base").GetProperty("ref").GetString();

    // basic labels
    if (action == "opened")
    {
        if (!labels.Contains("S: Requires Review"))
            await AddLabel(client, owner, repoName, number, labelStatusRequireReview);

        if (labels.Count == 0)
            await AddLabel(client, owner, repoName, number, labelStatusUntriaged);

        if (targetBranch == "stable" && !labels.Contains(labelBranchStable))
            await AddLabel(client, owner, repoName, number, labelBranchStable);
        else if (targetBranch == "staging" && !labels.Contains(labelBranchStaging))
            await AddLabel(client, owner, repoName, number, labelBranchStaging);
    }

    var changedFiles = await GetChangedFiles(client, owner, repoName, number);

    var matcher = new Matcher();
    matcher.AddInclude("**/*.rsi/*.png");            // Sprites
    matcher.AddInclude("Resources/Maps/**/*.yml");   // Map
    matcher.AddInclude("Resources/Prototypes/Maps/**/*.yml");
    matcher.AddInclude("**/*.xaml*");                // UI
    matcher.AddInclude("**/*.swsl");                 // Shaders
    matcher.AddInclude("**/*.ogg");                  // Audio

    var sprites = new Matcher().AddInclude("**/*.rsi/*.png");
    var maps = new Matcher().AddInclude("Resources/Maps/**/*.yml").AddInclude("Resources/Prototypes/Maps/**/*.yml");
    var ui = new Matcher().AddInclude("**/*.xaml*");
    var shaders = new Matcher().AddInclude("**/*.swsl");
    var audio = new Matcher().AddInclude("**/*.ogg");
    var cs = new Matcher().AddInclude("**/*.cs");

    if (sprites.Match(changedFiles).HasMatches)
        await AddLabel(client, owner, repoName, number, labelChangesSprites);

    if (maps.Match(changedFiles).HasMatches)
        await AddLabel(client, owner, repoName, number, labelChangesMap);

    if (ui.Match(changedFiles).HasMatches)
        await AddLabel(client, owner, repoName, number, labelChangesUi);

    if (shaders.Match(changedFiles).HasMatches)
        await AddLabel(client, owner, repoName, number, labelChangesShaders);

    if (audio.Match(changedFiles).HasMatches)
        await AddLabel(client, owner, repoName, number, labelChangesAudio);

    if (!cs.Match(changedFiles).HasMatches)
        await AddLabel(client, owner, repoName, number, labelChangesNoCSharp);
}

string ToHmacSha256(byte[] data, string secret)
{
    var key = Encoding.UTF8.GetBytes(secret);
    using var hmac = new HMACSHA256(key);
    var hashBytes = hmac.ComputeHash(data);
    return Convert.ToHexString(hashBytes).ToLowerInvariant();
}

async Task<List<string>> GetChangedFiles(HttpClient client, string owner, string repo, int prNumber)
{
    // TODO: Ratelimit? Might explode on big PRs???

    var files = new List<string>();
    var page = 1;
    while (true)
    {
        var res = await client.GetAsync($"{githubApiBase}/repos/{owner}/{repo}/pulls/{prNumber}/files?per_page=100&page={page}");
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

async Task AddLabel(HttpClient client, string owner, string repo, int number, string label)
{
    var request = new AddLabelRequest { labels = [label] };
    var json = JsonSerializer.Serialize(request, GitHubJsonContext.Default.AddLabelRequest);
    var content = new StringContent(json, Encoding.UTF8, "application/json");

    await client.PostAsync($"{githubApiBase}/repos/{owner}/{repo}/issues/{number}/labels", content);
}

async Task RemoveLabel(HttpClient client, string owner, string repo, int number, string label)
{
    await client.DeleteAsync($"{githubApiBase}/repos/{owner}/{repo}/issues/{number}/labels/{Uri.EscapeDataString(label)}");
}

[JsonSerializable(typeof(AddLabelRequest))]
public partial class GitHubJsonContext : JsonSerializerContext
{
}

public class AddLabelRequest
{
    // ReSharper disable once InconsistentNaming
    public string[] labels { get; set; } = [];
}