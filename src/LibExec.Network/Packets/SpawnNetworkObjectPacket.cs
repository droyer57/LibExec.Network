namespace LibExec.Network;

[Packet]
internal sealed class SpawnNetworkObjectPacket
{
    public uint Id { get; set; }
    public int OwnerId { get; set; }
    public NetworkObjectType Type { get; set; }
    public int Test { get; set; }
}