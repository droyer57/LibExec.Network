using LiteNetLib.Utils;

namespace LibExec.Network;

public sealed partial class InvokeMethodPacket : Packet
{
    [PacketProperty] private byte _methodId;
    [PacketProperty] private uint _networkObjectId;

    protected override void Serialize(NetDataWriter writer)
    {
        writer.Put(_networkObjectId);
        writer.Put(_methodId);
    }

    protected override void Deserialize(NetDataReader reader)
    {
        _networkObjectId = reader.GetUInt();
        _methodId = reader.GetByte();
    }
}