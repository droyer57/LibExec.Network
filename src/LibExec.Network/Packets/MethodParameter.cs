using LiteNetLib.Utils;

namespace LibExec.Network;

public struct MethodParameter : INetSerializable
{
    private static NetworkManager NetworkManager => NetworkManager.Instance;

    private Type _type;
    private object _value;

    public Type Type
    {
        get => _type;
        init => _type = value;
    }

    public object Value
    {
        get => _value;
        init => _value = value;
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(NetworkManager.Types.Get(Type));
        NetworkManager.NetWriterActions[Type].Invoke(writer, Value);
    }

    public void Deserialize(NetDataReader reader)
    {
        _type = NetworkManager.Types.Get(reader.GetByte());
        _value = NetworkManager.NetReaderActions[Type].Invoke(reader);
    }
}