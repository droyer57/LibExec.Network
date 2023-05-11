namespace LibExec.Network;

public sealed partial class NetworkManager
{
    public const string LocalAddress = "localhost";
    public const int DefaultPort = 1995;
    private readonly Dictionary<Type, Func<NetworkObject>> _networkObjectsCache = new();
    private readonly Dictionary<Type, Func<object>> _packetsCache = new();

    public NetworkManager()
    {
        // todo: helper class for reflection

        if (Instance != null)
        {
            throw new Exception($"{nameof(NetworkManager)} can only have one instance");
        }

        NetPacketProcessor.RegisterNestedType(() => new NetworkObjectType());

        Instance = this;
        ServerManager = new ServerManager();
        ClientManager = new ClientManager();

        InitInternal();
    }

    public int Port { get; private set; } = DefaultPort;

    public ServerManager ServerManager { get; }
    public ClientManager ClientManager { get; }

    public static NetworkManager Instance { get; private set; } = null!;

    public bool IsServer => ServerManager.ConnectionState == ConnectionState.Started;
    public bool IsClient => ClientManager.ConnectionState == ConnectionState.Started;
    public bool IsClientOnly => !IsServer && IsClient;

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

    public IEnumerable<T> Query<T>() where T : NetworkObject
    {
        return NetworkObjects.Values.OfType<T>();
    }

    public void RegisterNetworkObject<T>(Func<T> creator) where T : NetworkObject
    {
        _networkObjectsCache.Add(typeof(T), creator);
    }

    public void RegisterPacket<T>(Func<T> creator) where T : class
    {
        _packetsCache.Add(typeof(T), creator);
    }
}