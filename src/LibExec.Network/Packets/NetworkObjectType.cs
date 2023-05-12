using LiteNetLib.Utils;

namespace LibExec.Network;

internal struct NetworkObjectType : INetSerializable
{
    private Type _value;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(NetworkManager.Instance.NetworkObjectTypes.Get(_value));
    }

    public void Deserialize(NetDataReader reader)
    {
        _value = NetworkManager.Instance.NetworkObjectTypes.Get(reader.GetUShort());
    }

    public static implicit operator NetworkObjectType(Type type)
    {
        return new NetworkObjectType { _value = type };
    }

    public static implicit operator Type(NetworkObjectType reference)
    {
        return reference._value;
    }
}