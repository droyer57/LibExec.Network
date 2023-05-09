using System.Reflection;

namespace LibExec.Network;

public sealed class NetworkManager
{
    internal const string NetworkInitClassName = "InternalNetworkInit";
    internal const string Key = "DDurBXaw8sLsYs9x";
    public const string LocalAddress = "localhost";
    public const int DefaultPort = 1995;
    private readonly Dictionary<Type, Func<NetworkObject>> _networkObjectsCache = new();
    private readonly Dictionary<Type, Func<Packet>> _packetsCache = new();
    internal readonly Dictionary<Type, Action<Packet>> Callbacks = new();
    internal readonly Dictionary<uint, NetworkObject> NetworkObjects = new();
    internal readonly BiDictionary<Type> NetworkObjectTypes;
    internal readonly BiDictionary<Type> PacketTypes;

    public NetworkManager()
    {
        // todo: helper class for reflection

        if (Instance != null)
        {
            throw new Exception($"{nameof(NetworkManager)} can only have one instance");
        }

        Instance = this;
        ServerManager = new ServerManager();
        ClientManager = new ClientManager();

        var entryAssembly = Assembly.GetEntryAssembly() ?? throw new Exception("Cannot get entry assembly");
        var executingAssembly = Assembly.GetExecutingAssembly();

        var networkObjectTypes = entryAssembly.GetTypes().Where(x => x.BaseType == typeof(NetworkObject)).ToArray();
        var packetTypes = executingAssembly.GetTypes().Where(x => x.BaseType == typeof(Packet)).ToArray();

        NetworkObjectTypes = new BiDictionary<Type>(networkObjectTypes);
        PacketTypes = new BiDictionary<Type>(packetTypes);

        PlayerType = networkObjectTypes.FirstOrDefault(x => x.GetCustomAttribute<NetworkPlayerAttribute>() != null);

        Activator.CreateInstance(typeof(InternalNetworkInit), true);
        var networkInitClassType = entryAssembly.GetTypes().First(x => x.Name == NetworkInitClassName);
        Activator.CreateInstance(networkInitClassType, true);
    }

    internal Type? PlayerType { get; private set; }

    public int Port { get; private set; } = DefaultPort;

    public ServerManager ServerManager { get; }
    public ClientManager ClientManager { get; }

    public static NetworkManager Instance { get; private set; } = null!;

    public bool IsServer => ServerManager.ConnectionState == ConnectionState.Started;
    public bool IsClient => ClientManager.ConnectionState == ConnectionState.Started;

    public event Action<NetworkObject>? SpawnNetworkObjectEvent;
    public event Action<NetworkObject>? DestroyNetworkObjectEvent;

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

    internal NetworkObject CreateNetworkObject(Type type)
    {
        if (!_networkObjectsCache.TryGetValue(type, out var creator))
        {
            throw new InvalidOperationException($"{nameof(CreateNetworkObject)}");
        }

        return creator();
    }

    internal Packet CreatePacket(Type type)
    {
        if (!_packetsCache.TryGetValue(type, out var creator))
        {
            throw new InvalidOperationException($"{nameof(CreatePacket)}");
        }

        return creator();
    }

    internal void RegisterPacket<T>(Action<T> callback) where T : Packet
    {
        Callbacks.Add(typeof(T), x => callback((T)x));
    }

    public IEnumerable<T> Query<T>() where T : NetworkObject
    {
        return NetworkObjects.Values.OfType<T>();
    }

    public void RegisterNetworkObject<T>(Func<T> creator) where T : NetworkObject
    {
        _networkObjectsCache.Add(typeof(T), creator);
    }

    public void RegisterPacket<T>(Func<T> creator) where T : Packet
    {
        _packetsCache.Add(typeof(T), creator);
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
        SpawnNetworkObjectEvent?.Invoke(networkObject);
    }

    internal void RemoveNetworkObject(NetworkObject networkObject)
    {
        NetworkObjects.Remove(networkObject.Id);
        DestroyNetworkObjectEvent?.Invoke(networkObject);
    }
}