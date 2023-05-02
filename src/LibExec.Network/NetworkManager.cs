using System.Linq.Expressions;
using System.Reflection;

namespace LibExec.Network;

public sealed class NetworkManager
{
    internal const string Key = "DDurBXaw8sLsYs9x";
    public const string LocalAddress = "localhost";
    public const int DefaultPort = 1995;
    private readonly Dictionary<Type, Func<NetworkObject>> _networkObjectsCache;
    private readonly Dictionary<Type, Func<Packet>> _packetsCache;
    internal readonly Dictionary<Type, Action<Packet>> Callbacks = new();
    internal readonly BiDictionary<Type> NetworkObjectTypes;
    internal readonly BiDictionary<Type> PacketTypes;

    public NetworkManager(Type playerType)
    {
        PlayerType = playerType;

        if (Instance != null)
        {
            throw new Exception($"{nameof(NetworkManager)} can only have one instance");
        }

        Instance = this;
        ServerManager = new ServerManager();
        ClientManager = new ClientManager();

        var callingAssembly = Assembly.GetCallingAssembly();
        var executingAssembly = Assembly.GetExecutingAssembly();

        var networkObjetTypes = callingAssembly.GetTypes().Where(x => x.BaseType == typeof(NetworkObject)).ToArray();
        var packetTypes = executingAssembly.GetTypes().Where(x => x.BaseType == typeof(Packet)).ToArray();

        _networkObjectsCache = networkObjetTypes.ToDictionary(x => x, GetCreator<NetworkObject>);
        NetworkObjectTypes = new BiDictionary<Type>(networkObjetTypes);

        _packetsCache = packetTypes.ToDictionary(x => x, GetCreator<Packet>);
        PacketTypes = new BiDictionary<Type>(packetTypes);
    }

    internal Type PlayerType { get; private set; }

    public int Port { get; private set; } = DefaultPort;

    public ServerManager ServerManager { get; }
    public ClientManager ClientManager { get; }

    public static NetworkManager Instance { get; private set; } = null!;

    public void StartServer(int port)
    {
        if (ServerManager.IsRunning) return;

        Port = port;
        ServerManager.Start();
    }

    public void StartClient(string address, int port)
    {
        if (ClientManager.IsRunning) return;

        ClientManager.Address = address;
        Port = port;
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

    private static Func<T> GetCreator<T>(Type type)
    {
        return Expression.Lambda<Func<T>>(Expression.New(type)).Compile();
    }

    internal void RegisterPacket<T>(Action<T> callback) where T : Packet
    {
        Callbacks.Add(typeof(T), x => callback((T)x));
    }

    public IEnumerable<T> GetNetworkObjects<T>() where T : NetworkObject
    {
        if (ServerManager.IsRunning)
        {
            return ServerManager.NetworkObjects.Values.OfType<T>();
        }

        return ClientManager.NetworkObjects.Values.OfType<T>();
    }
}