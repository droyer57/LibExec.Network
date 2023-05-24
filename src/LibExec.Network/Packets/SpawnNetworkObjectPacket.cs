namespace LibExec.Network;

[Packet]
internal sealed class SpawnNetworkObjectPacket
{
    public uint Id { get; init; }
    public int OwnerId { get; init; }
    public NetworkObjectType Type { get; init; } = null!;
    public NetField[] Fields { get; init; } = null!;
    public NetProperty[] Properties { get; init; } = null!;
}