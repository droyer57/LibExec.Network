using LiteNetLib.Utils;

namespace LibExec.Network;

public struct NetField : INetSerializable
{
    private static NetworkManager NetworkManager => NetworkManager.Instance;

    public NetField(uint networkObjectId, ushort fieldId, object value)
    {
        NetworkObjectId = networkObjectId;
        FieldId = fieldId;
        Value = value;
    }

    public uint NetworkObjectId { get; private set; }
    public ushort FieldId { get; private set; }
    public object Value { get; private set; }

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(NetworkObjectId);
        writer.Put(FieldId);

        var type = NetworkManager.FieldParam[FieldId];
        NetworkManager.NetWriterActions[type].Invoke(writer, Value);
    }

    public void Deserialize(NetDataReader reader)
    {
        NetworkObjectId = reader.GetUInt();
        FieldId = reader.GetUShort();

        var type = NetworkManager.FieldParam[FieldId];
        Value = NetworkManager.NetReaderActions[type].Invoke(reader);
    }
}