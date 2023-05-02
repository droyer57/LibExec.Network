using LiteNetLib.Utils;

namespace LibExec.Network;

internal sealed partial class DestroyNetworkObjectPacket : Packet
{
    [PacketProperty] private uint _id;

    protected override void Serialize(NetDataWriter writer)
    {
        writer.Put(_id);
    }

    protected override void Deserialize(NetDataReader reader)
    {
        _id = reader.GetUInt();
    }
}