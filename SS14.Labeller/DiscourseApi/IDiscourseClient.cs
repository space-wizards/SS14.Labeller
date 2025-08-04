namespace SS14.Labeller.DiscourseApi;

public interface IDiscourseClient
{
    Task<string> CreateTopic(int category, string body, string title, CancellationToken ct);
}