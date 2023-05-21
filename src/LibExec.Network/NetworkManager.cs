using System.Net;
using System.Reflection;
using LiteNetLib.Utils;

namespace LibExec.Network;

public sealed class NetworkManager
{
    public const string LocalAddress = "localhost";
    public const int DefaultPort = 1995;

    internal const string Key = "DDurBXaw8sLsYs9x";
    private readonly Dictionary<Type, Func<NetworkObject>> _networkObjectsCache = new();
    private ushort _nextMethodId;

    public NetworkManager()
    {
        if (Instance != null)
        {
            throw new Exception($"{nameof(NetworkManager)} can only have one instance");
        }

        Instance = this;

        var _ = new Reflection();

        NetworkObjectTypes = new BiDictionary<Type>(Reflection.NetworkObjectTypes);

        ServerManager = new ServerManager();
        ClientManager = new ClientManager();

        AddMethods(Reflection.ServerMethodInfos);
        AddMethods(Reflection.ClientMethodInfos);
        AddMethods(Reflection.MulticastMethodInfos);

        ushort nextId = 0;
        FieldInfos = Reflection.ReplicateFieldInfos.ToDictionary(_ => nextId, x => new FastFieldInfo(x, nextId++));

        RegisterTypes(typeof(byte), typeof(sbyte), typeof(short), typeof(ushort), typeof(int), typeof(uint),
            typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(bool), typeof(string), typeof(char),
            typeof(IPEndPoint), typeof(NetworkObject));

        InitNetActions();
    }

    #region Internal

    internal Dictionary<uint, NetworkObject> NetworkObjects { get; } = new();
    internal BiDictionary<Type> NetworkObjectTypes { get; }
    internal BiDictionary<Type, byte> Types { get; } = new(); // todo: useless for now ?
    internal Dictionary<Type, Action<NetDataWriter, object>> NetWriterActions { get; } = new();
    internal Dictionary<Type, Func<NetDataReader, object>> NetReaderActions { get; } = new();
    internal Dictionary<ushort, FastFieldInfo> FieldInfos { get; }
    internal Dictionary<ushort, FastMethodInfo> Methods { get; } = new();

    internal PacketProcessor PacketProcessor =>
        IsServer ? ServerManager.PacketProcessor : ClientManager.PacketProcessor;

    internal NetworkObject CreateNetworkObject(Type type)
    {
        if (!_networkObjectsCache.TryGetValue(type, out var creator))
        {
            throw new InvalidOperationException($"{nameof(CreateNetworkObject)}");
        }

        return creator();
    }

    internal void EnsureMethodIsCalledByServer()
    {
        if (!IsServer)
        {
            throw new Exception("This method can only be called by the server.");
        }
    }

    internal void AddNetworkObject(NetworkObject networkObject)
    {
        NetworkObjects.Add(networkObject.Id, networkObject);
        if (networkObject.IsOwner && networkObject.GetType() == Reflection.PlayerType) // todo: NetworkPlayer class ? 
        {
            LocalPlayer = networkObject;
        }

        networkObject.OnSpawn();
        NetworkObjectEvent?.Invoke(networkObject, Network.NetworkObjectEvent.Spawned);
    }

    internal void RemoveNetworkObject(NetworkObject networkObject)
    {
        NetworkObjects.Remove(networkObject.Id);
        networkObject.OnDestroy();
        NetworkObjectEvent?.Invoke(networkObject, Network.NetworkObjectEvent.Destroyed);
    }

    internal void OnInvokeMethod(InvokeMethodPacket packet)
    {
        var instance = NetworkObjects[packet.Method.NetworkObjectId];
        var method = Methods[packet.Method.MethodId];

        method.Invoke(instance, packet.Method.Args);
    }

    internal void OnUpdateField(UpdateFieldPacket packet)
    {
        var instance = NetworkObjects[packet.Field.NetworkObjectId];
        FieldInfos[packet.Field.Id].SetValue(instance, packet.Field.Value);
    }

    internal static void SendField(ushort id, uint networkObjectId, object? oldValue, object value)
    {
        if (value == oldValue)
        {
            return;
        }

        var packet = new UpdateFieldPacket(new NetField(networkObjectId, id, value));
        Instance.ServerManager.SendPacketToAll(packet, excludeLocalConnection: true);
    }

    internal void InvokeNetworkEvent()
    {
        NetworkEvent?.Invoke();
    }

    #endregion

    #region Public

    public static NetworkManager Instance { get; private set; } = null!;

    public int Port { get; private set; } = DefaultPort;

    public NetworkObject? LocalPlayer { get; private set; }
    public ServerManager ServerManager { get; }
    public ClientManager ClientManager { get; }

    public bool IsServer => ServerManager.IsStarted;
    public bool IsClient => ClientManager.IsStarted;
    public bool IsClientOnly => !IsServer && IsClient;
    public bool IsServerOnly => IsServer && !IsClient;
    public bool IsHost => IsServer && IsClient;
    public bool IsOffline => !IsServer && !IsClient;

    public event Action<NetworkObject, NetworkObjectEvent>? NetworkObjectEvent;
    public event Action? NetworkEvent;

    public void StartServer(int? port = null)
    {
        if (ServerManager.IsRunning) return;

        Port = port ?? DefaultPort;
        ServerManager.Start();
    }

    public void StartClient(string? address = null, int? port = null)
    {
        if (ClientManager.IsRunning) return;

        ClientManager.Address = address ?? LocalAddress;
        Port = port ?? DefaultPort;
        ClientManager.Start();
    }

    public void StartLocalClient()
    {
        StartClient(LocalAddress, Port);
    }

    public void StopClient()
    {
        ClientManager.Stop();
    }

    public void StopServer()
    {
        ServerManager.Stop();
    }

    public IEnumerable<T> Query<T>() where T : NetworkObject
    {
        return NetworkObjects.Values.OrderBy(x => x.Id).OfType<T>();
    }

    public void RegisterNetworkObject<T>() where T : NetworkObject, new()
    {
        _networkObjectsCache.Add(typeof(T), () => new T());
    }

    public void RegisterPacket<T>(Action<T> serverCallback, Action<T> clientCallback) where T : class, new()
    {
        ServerManager.RegisterPacket(serverCallback);
        ClientManager.RegisterPacket(clientCallback);
    }

    public void RegisterPacket<T>(Action<T, NetConnection> serverCallback, Action<T, NetConnection> clientCallback)
        where T : class, new()
    {
        ServerManager.RegisterPacket(serverCallback);
        ClientManager.RegisterPacket(clientCallback);
    }

    public void RemovePacket<T>()
    {
        ServerManager.RemovePacket<T>();
        ClientManager.RemovePacket<T>();
    }

    public T? GetLocalPlayer<T>() where T : NetworkObject
    {
        return LocalPlayer as T;
    }

    #endregion

    #region Private

    // ReSharper disable once UnusedMember.Local
    private static bool ServerPatch(NetworkObject instance, ushort methodId, object[] args)
    {
        if (Instance.IsClientOnly)
        {
            var packet = GetInvokeMethodPacket(methodId, instance, args);
            Instance.ClientManager.SendPacket(packet);
        }

        return Instance.IsServer;
    }

    // ReSharper disable once UnusedMember.Local
    private static bool MulticastPatch(NetworkObject instance, ushort methodId, object[] args)
    {
        if (Instance.IsClientOnly)
        {
            return true;
        }

        var packet = GetInvokeMethodPacket(methodId, instance, args);
        Instance.ServerManager.SendPacketToAll(packet, excludeLocalConnection: true);
        return true;
    }

    // ReSharper disable once UnusedMember.Local
    private static bool ClientPatch(NetworkObject instance, ushort methodId, object[] args)
    {
        if (Instance.IsServer && instance.Owner is { IsLocal: false })
        {
            var packet = GetInvokeMethodPacket(methodId, instance, args);
            instance.Owner.SendPacket(packet);
            return false;
        }

        return true;
    }

    private static InvokeMethodPacket GetInvokeMethodPacket(ushort methodId, NetworkObject networkObject,
        object[] args)
    {
        var netMethod = new NetMethod(methodId, networkObject.Id, args);
        return new InvokeMethodPacket(netMethod);
    }

    private void AddMethods(IEnumerable<MethodInfo> methods)
    {
        foreach (var method in methods)
        {
            Methods.Add(_nextMethodId, new FastMethodInfo(method, _nextMethodId++));
        }
    }

    private void RegisterTypes(params Type[] types)
    {
        foreach (var type in types)
        {
            Types.Add((byte)Types.Count, type);
        }
    }

    private void InitNetActions()
    {
        NetWriterActions.Add(typeof(byte), (writer, value) => writer.Put((byte)value));
        NetWriterActions.Add(typeof(sbyte), (writer, value) => writer.Put((sbyte)value));
        NetWriterActions.Add(typeof(short), (writer, value) => writer.Put((short)value));
        NetWriterActions.Add(typeof(ushort), (writer, value) => writer.Put((ushort)value));
        NetWriterActions.Add(typeof(int), (writer, value) => writer.Put((int)value));
        NetWriterActions.Add(typeof(uint), (writer, value) => writer.Put((uint)value));
        NetWriterActions.Add(typeof(long), (writer, value) => writer.Put((long)value));
        NetWriterActions.Add(typeof(ulong), (writer, value) => writer.Put((ulong)value));
        NetWriterActions.Add(typeof(float), (writer, value) => writer.Put((float)value));
        NetWriterActions.Add(typeof(double), (writer, value) => writer.Put((double)value));
        NetWriterActions.Add(typeof(bool), (writer, value) => writer.Put((bool)value));
        NetWriterActions.Add(typeof(string), (writer, value) => writer.Put((string)value));
        NetWriterActions.Add(typeof(char), (writer, value) => writer.Put((char)value));
        NetWriterActions.Add(typeof(IPEndPoint), (writer, value) => writer.Put((IPEndPoint)value));

        NetReaderActions.Add(typeof(byte), reader => reader.GetByte());
        NetReaderActions.Add(typeof(sbyte), reader => reader.GetSByte());
        NetReaderActions.Add(typeof(short), reader => reader.GetShort());
        NetReaderActions.Add(typeof(ushort), reader => reader.GetUShort());
        NetReaderActions.Add(typeof(int), reader => reader.GetInt());
        NetReaderActions.Add(typeof(uint), reader => reader.GetUInt());
        NetReaderActions.Add(typeof(long), reader => reader.GetLong());
        NetReaderActions.Add(typeof(ulong), reader => reader.GetULong());
        NetReaderActions.Add(typeof(float), reader => reader.GetFloat());
        NetReaderActions.Add(typeof(double), reader => reader.GetDouble());
        NetReaderActions.Add(typeof(bool), reader => reader.GetBool());
        NetReaderActions.Add(typeof(string), reader => reader.GetString());
        NetReaderActions.Add(typeof(char), reader => reader.GetChar());
        NetReaderActions.Add(typeof(IPEndPoint), reader => reader.GetNetEndPoint());
    }

    #endregion
}