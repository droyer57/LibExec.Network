using System.Net;
using System.Reflection;
using LiteNetLib.Utils;

namespace LibExec.Network;

public sealed class NetworkManager
{
    public const string LocalAddress = "localhost";
    public const int DefaultPort = 1995;

    internal const string Key = "DDurBXaw8sLsYs9x";

    private readonly Dictionary<MethodInfo, Action<object, object[]?>> _methods;
    private readonly Dictionary<Type, Func<NetworkObject>> _networkObjectsCache = new();

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

        var methods = Reflection.ServerMethodInfos.Concat(Reflection.MulticastMethodInfos)
            .Concat(Reflection.ClientMethodInfos).ToArray();
        MethodInfos = new BiDictionary<MethodInfo>(methods);
        _methods = methods.ToDictionary(x => x, Reflection.CreateMethod);

        MethodsParams = methods.ToDictionary(x => x, x => x.GetParameters().Select(p => p.ParameterType).ToArray());

        RegisterTypes(typeof(byte), typeof(sbyte), typeof(short), typeof(ushort), typeof(int), typeof(uint),
            typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(bool), typeof(string), typeof(char),
            typeof(IPEndPoint), typeof(NetworkObject));

        InitNetActions();
    }

    #region Internal

    internal BiDictionary<MethodInfo> MethodInfos { get; }
    internal Dictionary<uint, NetworkObject> NetworkObjects { get; } = new();
    internal BiDictionary<Type> NetworkObjectTypes { get; }
    internal BiDictionary<Type, byte> Types { get; } = new(); // todo: useless for now ?
    internal Dictionary<Type, Action<NetDataWriter, object>> NetWriterActions { get; } = new();
    internal Dictionary<Type, Func<NetDataReader, object>> NetReaderActions { get; } = new();
    internal Dictionary<MethodInfo, Type[]> MethodsParams { get; }

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

        NetworkObjectEvent?.Invoke(networkObject, Network.NetworkObjectEvent.Spawned);
    }

    internal void RemoveNetworkObject(NetworkObject networkObject)
    {
        NetworkObjects.Remove(networkObject.Id);
        NetworkObjectEvent?.Invoke(networkObject, Network.NetworkObjectEvent.Destroyed);
    }

    internal void OnInvokeMethod(InvokeMethodPacket packet)
    {
        var instance = NetworkObjects[packet.Method.NetworkObjectId];
        var methodInfo = MethodInfos.Get(packet.Method.MethodId);
        var method = _methods[methodInfo];

        method.Invoke(instance, packet.Method.Args);
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

    internal static InvokeMethodPacket GetInvokeMethodPacket(MethodInfo methodInfo, NetworkObject networkObject,
        object[] args)
    {
        var methodId = Instance.MethodInfos.Get(methodInfo);
        var netMethod = new NetMethod(methodId, networkObject.Id, args);
        return new InvokeMethodPacket(netMethod);
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