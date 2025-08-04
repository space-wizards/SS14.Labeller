namespace SS14.Labeller.Messages;

public static class StatusMessages
{
    public const string CommentPostfix =
        """
        ---
        <sub>[Beep Boop](https://github.com/space-wizards/SS14.Labeller), this comment was made automatically.</sub>
        """;

    public const string DiscourseTopicBody =
        """
        {link}
        
        [poll type=regular results=always public=true chartType=bar groups=maintainers]
        # What to do?
        * Merge
        * Close
        * Other (Comment)
        [/poll]
        """;

    public const string StartedDiscussion =
        """
        A discussion thread has been opened.
        
        Please limit all further game design discussion to the following Topic:
        
        """;

    public const string UntriagedPullRequestMergedComment =
        """
        This pull request was merged without being triaged. Please consider triaging the pull request.
        
        For more information, review [our triage procedure](https://docs.spacestation14.com/en/wizden-staff/maintainer/triage-procedure.html)
        """;
}