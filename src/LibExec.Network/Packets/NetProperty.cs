using LiteNetLib.Utils;

namespace LibExec.Network;

public struct NetProperty : INetSerializable
{
    private static NetworkManager NetworkManager => NetworkManager.Instance;

    public NetProperty(uint networkObjectId, ushort id, object value)
    {
        NetworkObjectId = networkObjectId;
        Id = id;
        Value = value;
    }

    public uint NetworkObjectId { get; private set; }
    public ushort Id { get; private set; }
    public object Value { get; private set; }

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(NetworkObjectId);
        writer.Put(Id);

        var type = NetworkManager.PropertyInfos[Id].Type;
        writer.Put(type, Value);
    }

    public void Deserialize(NetDataReader reader)
    {
        NetworkObjectId = reader.GetUInt();
        Id = reader.GetUShort();

        var type = NetworkManager.PropertyInfos[Id].Type;
        Value = reader.Get(type);
    }
}