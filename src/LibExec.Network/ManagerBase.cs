using LiteNetLib;

namespace LibExec.Network;

public abstract class ManagerBase
{
    private readonly EventBasedNetListener _listener = new();
    protected readonly bool AsServer;
    protected readonly NetManager Manager;
    private ConnectionState _connectionState;

    protected ManagerBase()
    {
        Manager = new NetManager(_listener)
        {
            ChannelsCount = 6
        };
        _listener.ConnectionRequestEvent += OnConnectionRequest;
        _listener.PeerConnectedEvent += OnPeerConnected;
        _listener.PeerDisconnectedEvent += OnPeerDisconnected;
        _listener.NetworkReceiveEvent += OnNetworkReceive;
        AsServer = GetType() == typeof(ServerManager);

        NetworkManager.PacketProcessor.RegisterCallback<InvokeMethodPacket>(NetworkManager.OnInvokeMethod, Channel.Rpc,
            AsServer);
    }

    protected ServerManager ServerManager => NetworkManager.ServerManager;
    protected ClientManager ClientManager => NetworkManager.ClientManager;
    protected static NetworkManager NetworkManager => NetworkManager.Instance;

    public ConnectionState ConnectionState
    {
        get => _connectionState;
        internal set
        {
            if (_connectionState == value) return;

            _connectionState = value;
            ConnectionStateChangedEvent?.Invoke(_connectionState);
        }
    }

    public bool IsRunning => Manager.IsRunning;
    public bool IsStarted => _connectionState == ConnectionState.Started;
    internal int UpdateTime => Manager.UpdateTime;

    public event Action<ConnectionState>? ConnectionStateChangedEvent;

    internal void Update()
    {
        if (!Manager.IsRunning) return;

        Manager.PollEvents();
    }

    internal abstract void Start();

    internal virtual void Stop()
    {
        if (!Manager.IsRunning) return;

        ConnectionState = ConnectionState.Stopping;
        Manager.Stop();

        if (AsServer || !NetworkManager.IsServer)
        {
            NetworkManager.NetworkObjects.Clear();
        }
    }

    protected virtual void OnConnectionRequest(ConnectionRequest request)
    {
    }

    protected virtual void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
    }

    protected virtual void OnPeerConnected(NetPeer peer)
    {
    }

    protected virtual void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channel,
        DeliveryMethod deliveryMethod)
    {
        var connection = AsServer ? ServerManager.Connections[peer.Id] : ClientManager.Connection;

        if (channel == (byte)Channel.AllObjects)
        {
            ClientManager.OnReceiveAllObjects(reader);
        }
        else
        {
            NetworkManager.PacketProcessor.ReadAllPackets(reader, connection, channel, AsServer);
        }

        NetworkManager.InvokeNetworkEvent();
        reader.Recycle();
    }

    public void RegisterPacket<T>(Action<T> callback) where T : class, new()
    {
        NetworkManager.PacketProcessor.RegisterCallback(callback, Channel.Default, AsServer);
    }

    public void RegisterPacket<T>(Action<T, NetConnection> callback) where T : class, new()
    {
        NetworkManager.PacketProcessor.RegisterCallback(callback, Channel.Default, AsServer);
    }

    public bool RemovePacket<T>()
    {
        return NetworkManager.PacketProcessor.RemoveCallback<T>(AsServer);
    }
}