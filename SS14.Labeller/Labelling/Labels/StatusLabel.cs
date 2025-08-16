namespace SS14.Labeller.Labelling.Labels;

public class StatusLabel : LabelGenericBase<StatusLabel>
{
    private StatusLabel(string value) : base(value)
    {
    }

    /// <inheritdoc />
    public override bool AllowMultiple => true;

    const string Prefix = "S: ";

    public static readonly StatusLabel Untriaged = new(Prefix + "Untriaged");
    public static readonly StatusLabel Approved = new(Prefix + "Approved");
    public static readonly StatusLabel UndergoingDiscussion = new(Prefix + "Undergoing Discussion");
}