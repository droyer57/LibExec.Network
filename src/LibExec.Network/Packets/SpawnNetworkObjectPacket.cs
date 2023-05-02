using LiteNetLib.Utils;

namespace LibExec.Network;

internal sealed partial class SpawnNetworkObjectPacket : Packet
{
    [PacketProperty] private uint _id;
    [PacketProperty] private bool _isOwner;
    [PacketProperty] private Type _type = null!;

    protected override void Serialize(NetDataWriter writer)
    {
        writer.Put(NetworkManager.NetworkObjectTypes.Get(Type));
        writer.Put(Id);
        writer.Put(IsOwner);
    }

    protected override void Deserialize(NetDataReader reader)
    {
        _type = NetworkManager.NetworkObjectTypes.Get(reader.GetByte());
        _id = reader.GetUInt();
        _isOwner = reader.GetBool();
    }
}