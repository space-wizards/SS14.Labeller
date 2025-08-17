using System.Diagnostics.CodeAnalysis;

namespace SS14.Labeller.Labelling.Labels;

[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)]
public class StageOfWorkLabel : LabelGenericBase<StageOfWorkLabel>
{
    private const string Prefix = "S: ";

    private StageOfWorkLabel(string value) : base(value)
    {
    }

    public static readonly StageOfWorkLabel RequireReview = new(Prefix + "Needs Review"); // no idea why its called this
    public static readonly StageOfWorkLabel AwaitingChanges = new(Prefix + "Awaiting Changes");

    /// <inheritdoc />
    public override bool AllowMultiple => false;
}