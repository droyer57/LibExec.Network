using LiteNetLib.Utils;

namespace LibExec.Network;

internal sealed class NetworkObjectType : INetSerializable
{
    private Type _value = null!;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(NetworkManager.Instance.NetworkObjectTypes.Get(_value));
    }

    public void Deserialize(NetDataReader reader)
    {
        _value = NetworkManager.Instance.NetworkObjectTypes.Get(reader.GetByte());
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