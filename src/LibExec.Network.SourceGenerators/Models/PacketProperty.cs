namespace LibExec.Network.SourceGenerators.Models;

internal sealed class PacketProperty
{
    public string PropertyName { get; init; } = null!;
    public string TypeName { get; init; } = null!;
    public string BackingName { get; init; } = null!;
    public bool JsonIgnore { get; init; }
}