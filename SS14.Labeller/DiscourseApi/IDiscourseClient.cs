using SS14.Labeller.Models;

namespace SS14.Labeller.DiscourseApi;

public interface IDiscourseClient
{
    Task<DiscourseCreatedPost> CreateTopic(int category, string body, string title, CancellationToken ct);

    Task ApplyTags(int topicId, CancellationToken ct, params string[] tags);

    Task<DiscoursePost> GetTopic(int topicId, CancellationToken ct);
}