using System.Reflection;

namespace SS14.Labeller.Labelling.Labels;

public abstract class LabelBase(string value)
{
    public abstract bool AllowMultiple { get; }

    public string Value { get; } = value;

    public static implicit operator string(LabelBase myObject)
    {
        return myObject.Value;
    }

    protected abstract LabelBase[] GetAll();

    public bool TryGetFromString(string labelName, out LabelBase? foundLabel)
    {
        foundLabel = null;
        foreach (var label in GetAll())
        {
            if (labelName == label.Value)
            {
                foundLabel = label;
                return true;
            }
        }

        return false;
    }

    /// <inheritdoc />
    public override string ToString() => Value;
}

public abstract class LabelGenericBase<TLabel>(string value) : LabelBase(value)
    where TLabel : LabelBase
{
    private static TLabel[]? _cached;

    protected override TLabel[] GetAll()
    {
        _cached ??= GetType().GetFields(BindingFlags.Static | BindingFlags.Public)
                              .Where(x => x.FieldType == typeof(TLabel))
                              .Select(x => (TLabel)x.GetValue(null))
                              .ToArray();
        return _cached;
    }
}