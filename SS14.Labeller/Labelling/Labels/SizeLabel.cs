using System.Diagnostics.CodeAnalysis;

namespace SS14.Labeller.Labelling.Labels;

[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)]
public sealed class SizeLabel : LabelGenericBase<SizeLabel>
{
    private SizeLabel(string value) : base(value)
    {
    }

    const string Prefix = "size/";

    public static readonly SizeLabel XS = new(Prefix + "XS");
    public static readonly SizeLabel S = new(Prefix + "S");
    public static readonly SizeLabel M = new(Prefix + "M");
    public static readonly SizeLabel L = new(Prefix + "L");
    public static readonly SizeLabel XL = new(Prefix + "XL");

    private static readonly IReadOnlyDictionary<int, SizeLabel> Sizes = new Dictionary<int, SizeLabel>
    {
        { 0, XS },
        { 10, S },
        { 100, M },
        { 1000, L },
        { 5000, XL },
    };

    public static bool TryGetLabelFor(int totalDiff, [NotNullWhen(true)] out SizeLabel? label)
    {
        label = null;
        // ReSharper disable once LoopCanBeConvertedToQuery no fuck you, the resulting LINQ query is unreadable
        foreach (var kvp in Sizes.OrderByDescending(k => k.Key))
        {
            if (totalDiff < kvp.Key)
                continue;

            label = kvp.Value;
            return true;
        }

        return false;
    }

    /// <inheritdoc />
    public override bool AllowMultiple => false;
}