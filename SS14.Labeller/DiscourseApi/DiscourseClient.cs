using System.Text;
using System.Text.Json;
using SS14.Labeller.Models;

namespace SS14.Labeller.DiscourseApi;

public class DiscourseClient(HttpClient httpClient) : IDiscourseClient
{
    public async Task<string> CreateTopic(int category, string body, string title, CancellationToken ct)
    {
        var request = new CreatePostRequest
        {
            title = title,
            category = category,
            raw = body,
        };
        var json = JsonSerializer.Serialize(request, SourceGenerationContext.Default.CreatePostRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var postRes = await httpClient.PostAsync("posts.json", content, ct);
        postRes.EnsureSuccessStatusCode();
        var deserialized = (DiscoursePost)JsonSerializer.Deserialize(await postRes.Content.ReadAsStringAsync(ct), typeof(DiscoursePost), SourceGenerationContext.DeserializationContext)!;
        return deserialized.PostUrl;
    }
}

public class CreatePostRequest
{
    // ReSharper disable InconsistentNaming
    public required string title { get; set; }
    public required string raw { get; set; }
    public int category { get; set; }
    // ReSharper restore InconsistentNaming
}