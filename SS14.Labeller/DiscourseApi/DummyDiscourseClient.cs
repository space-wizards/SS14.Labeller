using SS14.Labeller.Models;

namespace SS14.Labeller.DiscourseApi;

#pragma warning disable CS9113 // Parameter is unread.
public class DummyDiscourseClient(HttpClient _) : IDiscourseClient
#pragma warning restore CS9113 // Parameter is unread.
{
    public Task<DiscourseCreatedPost> CreateTopic(int category, string body, string title, CancellationToken ct)
        => Task.FromResult(new DiscourseCreatedPost()
        {
            TopicId = -1,
            PostUrl = ""
        });

    public Task ApplyTags(int topicId, CancellationToken ct, params string[] tags)
        => Task.CompletedTask;

    public Task<DiscoursePost> GetTopic(int topicId, CancellationToken ct)
        => Task.FromResult<DiscoursePost>(new DiscoursePost()
        {
            TopicId = -1,
            CategoryId = -1,
            Title = "",
        });
}