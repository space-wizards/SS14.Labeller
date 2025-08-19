using System.Diagnostics.CodeAnalysis;

namespace SS14.Labeller.Labelling.Labels;

[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)]
public sealed class ChangesLabel : LabelGenericBase<ChangesLabel>
{
    private ChangesLabel(string value) : base(value)
    {
    }

    const string Prefix = "Changes: ";

    public static readonly ChangesLabel Audio = new(Prefix + "Audio");
    public static readonly ChangesLabel Map = new(Prefix + "Map");
    public static readonly ChangesLabel NoCSharp = new(Prefix + "No C#");
    public static readonly ChangesLabel Shaders = new(Prefix + "Shaders");
    public static readonly ChangesLabel Sprites = new(Prefix + "Sprites");
    public static readonly ChangesLabel Ui = new(Prefix + "UI");

    /// <inheritdoc />
    public override bool AllowMultiple => true;
}