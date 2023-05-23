namespace LibExec.Network;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class ReplicateAttribute : Attribute
{
    public ReplicateAttribute()
    {
    }

    public ReplicateAttribute(int condition)
    {
        Condition = condition;
    }

    public int Condition { get; }
    public string? OnChange { get; init; }
}