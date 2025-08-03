namespace SS14.Labeller.Labels;

public static class SizeLabels
{
    private static readonly IReadOnlyDictionary<int, string> Sizes = new Dictionary<int, string>()
    {
        { 0, Prefix + "XS" },
        { 10, Prefix + "S" },
        { 100, Prefix + "M" },
        { 1000, Prefix + "L" },
        { 5000, Prefix + "XL" },
    };

    public const string Prefix = "size/";

    public static string? TryGetLabelFor(int totalDiff)
    {
        string? sizeLabel = null;
        // ReSharper disable once LoopCanBeConvertedToQuery no fuck you, the resulting LINQ query is unreadable
        foreach (var kvp in Sizes.OrderByDescending(k => k.Key))
        {
            if (totalDiff < kvp.Key)
                continue;

            sizeLabel = kvp.Value;
            break;
        }

        return sizeLabel;
    }
}