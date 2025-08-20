using System.Text;
using System.Text.Json;
using SS14.Labeller.Models;

namespace SS14.Labeller.DiscourseApi;

public class DiscourseClient(HttpClient httpClient) : IDiscourseClient
{
    public async Task<DiscourseCreatedPost> CreateTopic(int category, string body, string title, CancellationToken ct)
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
        var deserialized = (DiscourseCreatedPost)JsonSerializer.Deserialize(await postRes.Content.ReadAsStringAsync(ct), typeof(DiscourseCreatedPost), SourceGenerationContext.DeserializationContext)!;
        return deserialized;
    }

    public async Task ApplyTags(int topicId, CancellationToken ct, params string[] tags)
    {
        // Ok so, i am not very well versed in the discourse api, especially given that this part is not documented, lmao.
        // so this might look very jank.

        // idk if we need to set the title key in the UpdatePostRequest. The request on the browser included it so im just gonna include it here as well.
        var topic = await GetTopic(topicId, ct);
        var request = new UpdatePostRequest()
        {
            category_id = topic.CategoryId,
            tags = tags,
            title = topic.Title!,
        };

        var json = JsonSerializer.Serialize(request, SourceGenerationContext.Default.UpdatePostRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var res = await httpClient.PutAsync($"t/-/{topicId}.json", content, ct);
        res.EnsureSuccessStatusCode();
    }

    public async Task<DiscoursePost> GetTopic(int topicId, CancellationToken ct)
    {
        var postRes = await httpClient.GetAsync($"t/{topicId}.json", ct);
        postRes.EnsureSuccessStatusCode();
        return (DiscoursePost)JsonSerializer.Deserialize(await postRes.Content.ReadAsStringAsync(ct), typeof(DiscoursePost), SourceGenerationContext.DeserializationContext)!;
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

public class UpdatePostRequest
{
    // ReSharper disable InconsistentNaming
    public required int category_id { get; set; }
    public required string[] tags { get; set; }
    public required string title { get; set; }
    // ReSharper restore InconsistentNaming
}