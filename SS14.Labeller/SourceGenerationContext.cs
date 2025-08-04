using System.Text.Json;
using System.Text.Json.Serialization;
using SS14.Labeller.DiscourseApi;
using SS14.Labeller.GitHubApi;
using SS14.Labeller.Models;

namespace SS14.Labeller;

[JsonSerializable(typeof(AddLabelRequest))]
[JsonSerializable(typeof(AddCommentRequest))]
[JsonSerializable(typeof(IssuesEvent))]
[JsonSerializable(typeof(PullRequestEvent))]
[JsonSerializable(typeof(PullRequestReviewEvent))]
[JsonSerializable(typeof(DiscoursePost))]
[JsonSerializable(typeof(CreatePostRequest))]
[JsonSerializable(typeof(IssueComment))]
[JsonSerializable(typeof(IssueComment[]))]
public partial class SourceGenerationContext : JsonSerializerContext
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static SourceGenerationContext DeserializationContext { get; } = new(JsonSerializerOptions);
}