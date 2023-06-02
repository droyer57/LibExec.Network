using System.Reflection;

namespace LibExec.Network;

public sealed class NetworkManager
{
    public const string LocalAddress = "localhost";
    public const int DefaultPort = 1995;

    internal const string Key = "DDurBXaw8sLsYs9x";
    private readonly Dictionary<ushort, Func<NetworkObject>> _networkObjectsCache = new();
    private ushort _nextMethodId;

    public NetworkManager()
    {
        if (Instance != null)
        {
            throw new Exception($"{nameof(NetworkManager)} can only have one instance");
        }

        Instance = this;

        var _ = new Reflection();

        AddMethods(Reflection.ServerMethodInfos);
        AddMethods(Reflection.ClientMethodInfos);
        AddMethods(Reflection.MulticastMethodInfos);

        ushort nextId = 1;
        NetworkObjectIds = Reflection.NetworkObjectTypes.ToDictionary(x => x, _ => nextId++);

        nextId = 0;
        var memberInfos = Reflection.ReplicateFieldInfos.Select(x => new FastMemberInfo(x, nextId++))
            .Concat(Reflection.ReplicatePropertyInfos.Select(x => new FastMemberInfo(x, nextId++))).ToArray();

        MemberInfos = memberInfos.ToDictionary(x => x.Id, x => x);
        MemberInfosByClassId =
            memberInfos.GroupBy(x => x.DeclaringClassId).ToDictionary(x => x.Key, x => x.AsEnumerable());

        PacketProcessor = new PacketProcessor();
        PacketProcessor.RegisterType<NetMethod>();
        PacketProcessor.RegisterType<NetMember>();

        ServerManager = new ServerManager();
        ClientManager = new ClientManager();
    }

    #region Internal

    internal Dictionary<uint, NetworkObject> NetworkObjects { get; } = new();
    internal Dictionary<Type, ushort> NetworkObjectIds { get; }
    internal Dictionary<ushort, FastMemberInfo> MemberInfos { get; }
    internal Dictionary<ushort, IEnumerable<FastMemberInfo>> MemberInfosByClassId { get; }
    internal Dictionary<ushort, FastMethodInfo> Methods { get; } = new();
    internal PacketProcessor PacketProcessor { get; }

    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    internal ushort PlayerClassId { get; set; } // set by the code generator

    internal NetworkObject CreateNetworkObject(ushort classId)
    {
        if (!_networkObjectsCache.TryGetValue(classId, out var creator))
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
        if (LocalPlayer == null! && networkObject.IsOwner && networkObject.ClassId == PlayerClassId)
        {
            LocalPlayer = networkObject;
        }
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

    internal void InvokeNetworkObjectSpawn(NetworkObject networkObject)
    {
        networkObject.OnSpawn();
        NetworkObjectEvent?.Invoke(networkObject, Network.NetworkObjectEvent.Spawned);
    }

    #endregion

    #region Public

    public static NetworkManager Instance { get; private set; } = null!;

    public int Port { get; private set; } = DefaultPort;

    public NetworkObject LocalPlayer { get; private set; } = null!;
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

    public void RegisterPacket<T>(Action<T, NetConnection> serverCallback, Action<T> clientCallback)
        where T : class, new()
    {
        ServerManager.RegisterPacket(serverCallback);
        ClientManager.RegisterPacket(clientCallback);
    }

    public void RegisterPacket<T>(Action<T> serverCallback, Action<T, NetConnection> clientCallback)
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

    public T GetLocalPlayer<T>() where T : NetworkObject
    {
        return (T)LocalPlayer;
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

    // ReSharper disable once UnusedMember.Local
    private void RegisterNetworkObject<T>(ushort classId) where T : NetworkObject, new()
    {
        _networkObjectsCache.Add(classId, () => new T());
    }

    #endregion
}