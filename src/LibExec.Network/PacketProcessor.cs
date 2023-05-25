using LiteNetLib.Utils;

namespace LibExec.Network;

internal sealed class PacketProcessor
{
    private readonly Dictionary<Type, RegisterDelegate> _clientCallbacks = new();

    private readonly NetSerializer _netSerializer = new();
    private readonly BiDictionary<Type> _packetTypes;
    private readonly BiDictionary<Type, byte> _packetTypesByChannel = new();
    private readonly Dictionary<Type, RegisterDelegate> _serverCallbacks = new();
    private readonly NetDataWriter _writer = new();

    public PacketProcessor()
    {
        _packetTypes = new BiDictionary<Type>(Reflection.PacketTypes);
    }

    private Dictionary<Type, RegisterDelegate> GetCallbacks(bool asServer)
    {
        return asServer ? _serverCallbacks : _clientCallbacks;
    }

    private RegisterDelegate GetCallback(NetDataReader reader, byte channel, bool asServer)
    {
        var type = channel == 0 ? GetHeader(reader) : _packetTypesByChannel.Get(channel);

        if (!GetCallbacks(asServer).TryGetValue(type, out var action))
        {
            throw new ParseException($"Undefined packet in {nameof(NetDataReader)}");
        }

        return action;
    }

    private Type GetHeader(NetDataReader reader)
    {
        return _packetTypes.Get(reader.GetUShort());
    }

    private void WriteHeader<T>()
    {
        _writer.Put(_packetTypes.Get(typeof(T)));
    }

    public NetDataWriter Write<T>(T packet, out byte channel) where T : class, new()
    {
        _writer.Reset();
        channel = _packetTypesByChannel.Get(typeof(T));
        if (channel == 0)
        {
            WriteHeader<T>();
        }

        _netSerializer.Serialize(_writer, packet);
        return _writer;
    }

    public void RegisterType<T>() where T : struct, INetSerializable
    {
        _netSerializer.RegisterNestedType<T>();
    }

    public void RegisterType<T>(Func<T> constructor) where T : class, INetSerializable
    {
        _netSerializer.RegisterNestedType(constructor);
    }

    public void ReadAllPackets(NetDataReader reader, NetConnection connection, byte channel, bool asServer)
    {
        while (reader.AvailableBytes > 0)
        {
            GetCallback(reader, channel, asServer).Invoke(reader, connection);
        }
    }

    public void RegisterCallback<T>(Action<T> onReceive, Channel channel, bool asServer) where T : class, new()
    {
        RegisterCallback<T>((packet, _) => onReceive(packet), channel, asServer);
    }

    public void RegisterCallback<T>(Action<T, NetConnection> onReceive, Channel channel, bool asServer)
        where T : class, new()
    {
        _netSerializer.Register<T>();
        var packet = new T();
        _packetTypesByChannel.TryAdd((byte)channel, typeof(T));
        GetCallbacks(asServer)[typeof(T)] = (reader, connection) =>
        {
            _netSerializer.Deserialize(reader, packet);
            onReceive(packet, connection);
        };
    }

    public bool RemoveCallback<T>(bool asServer)
    {
        return GetCallbacks(asServer).Remove(typeof(T));
    }

    private delegate void RegisterDelegate(NetDataReader reader, NetConnection connection);
}