namespace LibExec.Network;

[Packet]
internal sealed class InvokeMethodPacket
{
    public ushort MethodId { get; init; }
    public uint NetworkObjectId { get; init; }
    public MethodParameter[] Args { get; init; } = null!;
}