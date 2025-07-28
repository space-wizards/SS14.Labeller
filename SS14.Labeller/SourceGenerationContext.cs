using System.Text.Json.Serialization;
using SS14.Labeller.GitHubApi;
using SS14.Labeller.Models;

namespace SS14.Labeller;

[JsonSerializable(typeof(AddLabelRequest))]
[JsonSerializable(typeof(IssuesEvent))]
[JsonSerializable(typeof(PullRequestEvent))]
[JsonSerializable(typeof(PullRequestReviewEvent))]
public partial class SourceGenerationContext : JsonSerializerContext
{
}