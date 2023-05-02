using LiteNetLib.Utils;

namespace LibExec.Network;

// todo: make this class public to allow user to send packet 
internal abstract class Packet
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