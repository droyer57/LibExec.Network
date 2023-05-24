using System.Reflection;

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

        PacketProcessor = new PacketProcessor();
        PacketProcessor.RegisterType<NetworkObjectType>();
        PacketProcessor.RegisterType<NetMethod>();
        PacketProcessor.RegisterType<NetField>();

        ServerManager = new ServerManager();
        ClientManager = new ClientManager();

        AddMethods(Reflection.ServerMethodInfos);
        AddMethods(Reflection.ClientMethodInfos);
        AddMethods(Reflection.MulticastMethodInfos);

        ushort nextId = 0;
        FieldInfos = Reflection.ReplicateFieldInfos.ToDictionary(_ => nextId, x => new FastFieldInfo(x, nextId++));
        FieldInfosByType = FieldInfos.Values.GroupBy(x => x.DeclaringType)
            .ToDictionary(x => x.Key, x => x.AsEnumerable());
    }

    #region Internal

    internal Dictionary<uint, NetworkObject> NetworkObjects { get; } = new();
    internal BiDictionary<Type> NetworkObjectTypes { get; }
    internal Dictionary<ushort, FastFieldInfo> FieldInfos { get; }
    internal Dictionary<Type, IEnumerable<FastFieldInfo>> FieldInfosByType { get; }
    internal Dictionary<ushort, FastMethodInfo> Methods { get; } = new();
    internal PacketProcessor PacketProcessor { get; }

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
    public int UpdateTime => ServerManager.UpdateTime;

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

    public void Update()
    {
        ServerManager.Update();
        ClientManager.Update();
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

    #endregion
}