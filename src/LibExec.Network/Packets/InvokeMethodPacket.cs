namespace LibExec.Network;

[Packet]
internal sealed class InvokeMethodPacket
{
    public byte MethodId { get; init; }
    public uint NetworkObjectId { get; init; }
}