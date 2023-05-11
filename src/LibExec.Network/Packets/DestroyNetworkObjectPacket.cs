namespace LibExec.Network;

[Packet]
internal sealed class DestroyNetworkObjectPacket
{
    public uint Id { get; init; }
}