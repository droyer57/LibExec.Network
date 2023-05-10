using LiteNetLib.Utils;

namespace LibExec.Network;

internal sealed partial class SpawnNetworkObjectPacket : Packet
{
    [PacketProperty] private uint _id;
    [PacketProperty] private int _ownerId;
    [PacketProperty] private Type _type = null!;

    protected override void Serialize(NetDataWriter writer)
    {
        writer.Put(NetworkManager.NetworkObjectTypes.Get(Type));
        writer.Put(Id);
        writer.Put(OwnerId);
    }

    protected override void Deserialize(NetDataReader reader)
    {
        _type = NetworkManager.NetworkObjectTypes.Get(reader.GetByte());
        _id = reader.GetUInt();
        _ownerId = reader.GetInt();
    }
}