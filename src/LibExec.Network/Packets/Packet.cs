using LiteNetLib.Utils;

namespace LibExec.Network;

public class Packet
{
    protected static NetworkManager NetworkManager => NetworkManager.Instance;

    protected virtual void Serialize(NetDataWriter writer)
    {
    }

    protected virtual void Deserialize(NetDataReader reader)
    {
    }

    public byte[] GetData()
    {
        var writer = new NetDataWriter();
        writer.Put(NetworkManager.PacketTypes.Get(GetType()));
        Serialize(writer);

        return writer.Data;
    }

    internal void DeserializeInternal(NetDataReader reader)
    {
        Deserialize(reader);
    }
}