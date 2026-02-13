namespace SS14.Labeller.Messages;

public static class StatusMessages
{
    public const string CommentPostfix =
        """
        ---
        <sub>[Beep Boop](https://github.com/space-wizards/SS14.Labeller), this comment was made automatically.</sub>
        """;

    public static string DiscourseTopicBody(string link) =>
        $"""
         {link}
         
         Please use this thread to discuss the merits of the linked PR. Maintainers are expected to 
         weigh the entire discussion, to the best of their ability, when making resolutions about the PR.
         """;

    public static string StartedDiscussion(string topicName) =>
        $"""
         A discussion thread has been opened.
         
         Please limit all further game design discussion to the following Topic: {topicName}
         
         """;

    public const string UntriagedPullRequestMergedComment =
        """
        This pull request was merged without being triaged. Please consider triaging the pull request.
        
        For more information, review [our triage procedure](https://docs.spacestation14.com/en/wizden-staff/maintainer/triage-procedure.html)
        """;
}