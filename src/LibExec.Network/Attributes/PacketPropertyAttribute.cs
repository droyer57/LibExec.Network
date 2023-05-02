namespace LibExec.Network;

[AttributeUsage(AttributeTargets.Field)]
public sealed class PacketPropertyAttribute : Attribute
{
    public bool JsonIgnore { get; set; }
}