using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using HarmonyLib;
using LiteNetLib;

namespace LibExec.Network;

public sealed class NetworkManager
{
    public const string LocalAddress = "localhost";
    public const int DefaultPort = 1995;

    internal const string Key = "DDurBXaw8sLsYs9x";

    private readonly Harmony _harmony = new(Key);
    private readonly Dictionary<MethodInfo, Action<object, object[]?>> _methods = new();
    private readonly BiDictionary<MethodInfo, ushort> _methodTypes;
    private readonly Dictionary<Type, Func<NetworkObject>> _networkObjectsCache = new();

    public NetworkManager()
    {
        if (Instance != null)
        {
            throw new Exception($"{nameof(NetworkManager)} can only have one instance");
        }

        Instance = this;

        var _ = new Reflection();

        PacketProcessor = new PacketProcessor();
        PacketProcessor.RegisterType<NetworkObjectType>();

        NetworkObjectTypes = new BiDictionary<Type, ushort>(Reflection.NetworkObjectTypes);

        ServerManager = new ServerManager();
        ClientManager = new ClientManager();

        RegisterPacket<InvokeMethodPacket>(OnInvokeMethod);

        _methodTypes = new BiDictionary<MethodInfo, ushort>(Reflection.ServerMethodInfos);
        foreach (var method in Reflection.ServerMethodInfos)
        {
            _methods.Add(method, Reflection.CreateMethod(method));
        }

        PatchServerMethods();
    }

    internal Dictionary<uint, NetworkObject> NetworkObjects { get; } = new();
    internal BiDictionary<Type, ushort> NetworkObjectTypes { get; private set; }
    internal PacketProcessor PacketProcessor { get; }

    public int Port { get; private set; } = DefaultPort;

    public ServerManager ServerManager { get; }
    public ClientManager ClientManager { get; }

    public static NetworkManager Instance { get; private set; } = null!;

    public bool IsServer => ServerManager.IsStarted;
    public bool IsClient => ClientManager.IsStarted;
    public bool IsClientOnly => !IsServer && IsClient;
    public bool IsServerOnly => IsServer && !IsClient;
    public bool IsHost => IsServer && IsClient;
    public bool IsOffline => !IsServer && !IsClient;

    public event Action<NetworkObject, NetworkObjectEventState>? NetworkObjectEvent;

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
        return NetworkObjects.Values.OfType<T>();
    }

    public void RegisterNetworkObject<T>() where T : NetworkObject, new()
    {
        _networkObjectsCache.Add(typeof(T), () => new T());
    }

    private void PatchServerMethods()
    {
        var serverPatch = GetType().GetMethodWithName(nameof(ServerPatch));
        foreach (var method in Reflection.ServerMethodInfos)
        {
            _harmony.Patch(method, new HarmonyMethod(serverPatch));
        }
    }

    internal NetworkObject CreateNetworkObject(Type type)
    {
        if (!_networkObjectsCache.TryGetValue(type, out var creator))
        {
            throw new InvalidOperationException($"{nameof(CreateNetworkObject)}");
        }

        return creator();
    }

    public void RegisterPacket<T>(Action<T> callback) where T : class, new()
    {
        PacketProcessor.RegisterCallback(callback);
    }

    public void RegisterPacket<T>(Action<T, NetPeer> callback) where T : class, new()
    {
        PacketProcessor.RegisterCallback(callback);
    }

    public void RemovePacket<T>()
    {
        PacketProcessor.RemoveCallback<T>();
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
        NetworkObjectEvent?.Invoke(networkObject, NetworkObjectEventState.Created);
    }

    internal void RemoveNetworkObject(NetworkObject networkObject)
    {
        NetworkObjects.Remove(networkObject.Id);
        NetworkObjectEvent?.Invoke(networkObject, NetworkObjectEventState.Destroyed);
    }

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    private static bool ServerPatch(NetworkObject __instance, MethodInfo __originalMethod, object[] __args)
    {
        if (Instance.IsClientOnly)
        {
            var methodId = Instance._methodTypes.Get(__originalMethod);
            var packet = new InvokeMethodPacket { NetworkObjectId = __instance.Id, MethodId = methodId };
            Instance.ClientManager.Peer.SendPacket(packet);
        }

        return Instance.IsServer;
    }

    private void OnInvokeMethod(InvokeMethodPacket packet)
    {
        var instance = NetworkObjects[packet.NetworkObjectId];
        var methodInfo = _methodTypes.Get(packet.MethodId);
        var method = _methods[methodInfo];

        method.Invoke(instance, null);
    }
}