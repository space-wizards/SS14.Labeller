namespace SS14.Labeller.Labelling.Labels;

public sealed class BranchLabel : LabelGenericBase<BranchLabel>
{
    private BranchLabel(string value) : base(value)
    {
    }

    const string Prefix = "Branch: ";

    public static readonly BranchLabel Stable = new(Prefix + "Stable");
    public static readonly BranchLabel Staging = new(Prefix + "Staging");

    /// <inheritdoc />
    public override bool AllowMultiple => false;
}