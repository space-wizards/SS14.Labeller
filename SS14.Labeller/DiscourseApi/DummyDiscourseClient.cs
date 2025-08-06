using SS14.Labeller.Models;

namespace SS14.Labeller.DiscourseApi;

public class DummyDiscourseClient : IDiscourseClient
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