namespace LibExec.Network;

[Packet]
internal sealed class SpawnNetworkObjectPacket
{
    public uint Id { get; init; }
    public int OwnerId { get; init; }
    public ushort ClassId { get; init; }
    public NetMember[] Members { get; init; } = null!;
}