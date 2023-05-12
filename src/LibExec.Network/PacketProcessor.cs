using LiteNetLib.Utils;

namespace LibExec.Network;

public sealed class PacketProcessor
{
    private readonly Dictionary<Type, RegisterDelegate> _callbacks = new();

    private readonly NetSerializer _netSerializer = new();
    private readonly BiDictionary<Type> _packetTypes;

    public PacketProcessor()
    {
        _packetTypes = new BiDictionary<Type>(Reflection.PacketTypes);
    }

    private RegisterDelegate GetCallback(NetDataReader reader)
    {
        var header = _packetTypes.Get(reader.GetByte());
        if (!_callbacks.TryGetValue(header, out var action))
        {
            throw new ParseException("Undefined packet in NetDataReader");
        }

        return action;
    }

    private void WriteHeader<T>(NetDataWriter writer)
    {
        writer.Put(_packetTypes.Get(typeof(T)));
    }

    public void RegisterType<T>() where T : struct, INetSerializable
    {
        _netSerializer.RegisterNestedType<T>();
    }

    public void RegisterType<T>(Func<T> constructor) where T : class, INetSerializable
    {
        _netSerializer.RegisterNestedType(constructor);
    }

    public void ReadAllPackets(NetDataReader reader)
    {
        while (reader.AvailableBytes > 0)
        {
            ReadPacket(reader);
        }
    }

    private void ReadPacket(NetDataReader reader)
    {
        GetCallback(reader)(reader);
    }

    public void Write<T>(NetDataWriter writer, T packet) where T : class, new()
    {
        WriteHeader<T>(writer);
        _netSerializer.Serialize(writer, packet);
    }

    public void RegisterCallback<T>(Action<T> onReceive) where T : class, new()
    {
        _netSerializer.Register<T>();
        var packet = new T();
        _callbacks[typeof(T)] = reader =>
        {
            _netSerializer.Deserialize(reader, packet);
            onReceive(packet);
        };
    }

    public bool RemoveCallback<T>()
    {
        return _callbacks.Remove(typeof(T));
    }

    private delegate void RegisterDelegate(NetDataReader reader);
}