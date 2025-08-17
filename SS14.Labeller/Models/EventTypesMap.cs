using System.Diagnostics.CodeAnalysis;

namespace SS14.Labeller.Models;

public static class EventTypesMap
{
    private static readonly Dictionary<string, Type> Matches = new Dictionary<string, Type>
    {
        [PullRequestEvent.EventTypeName] = typeof(PullRequestEvent),
        [PullRequestReviewEvent.EventTypeName] = typeof(PullRequestReviewEvent),
        [IssuesEvent.EventTypeName] = typeof(IssuesEvent),
    };

    public static bool TryGetValue(string eventTypeName, [NotNullWhen(true)] out Type? eventType)
    {
        return Matches.TryGetValue(eventTypeName, out eventType);
    }
}