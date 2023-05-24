using LiteNetLib;

namespace LibExec.Network;

public abstract class ManagerBase
{
    private readonly EventBasedNetListener _listener = new();
    protected readonly bool IsServer;
    protected readonly NetManager Manager;
    private ConnectionState _connectionState;

    protected ManagerBase()
    {
        Manager = new NetManager(_listener)
        {
            ChannelsCount = 5
        };
        _listener.ConnectionRequestEvent += OnConnectionRequest;
        _listener.PeerConnectedEvent += OnPeerConnected;
        _listener.PeerDisconnectedEvent += OnPeerDisconnected;
        _listener.NetworkReceiveEvent += OnNetworkReceive;
        IsServer = GetType() == typeof(ServerManager);

        NetworkManager.PacketProcessor.RegisterCallback<InvokeMethodPacket>(NetworkManager.OnInvokeMethod, Channel.Rpc,
            IsServer);
    }

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
        NetworkManager.NetworkObjects.Clear();
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
        NetworkManager.PacketProcessor.ReadAllPackets(reader, new NetConnection(peer), channel, IsServer);
        NetworkManager.InvokeNetworkEvent();
        reader.Recycle();
    }

    public void RegisterPacket<T>(Action<T> callback) where T : class, new()
    {
        NetworkManager.PacketProcessor.RegisterCallback(callback, Channel.Default, IsServer);
    }

    public void RegisterPacket<T>(Action<T, NetConnection> callback) where T : class, new()
    {
        NetworkManager.PacketProcessor.RegisterCallback(callback, Channel.Default, IsServer);
    }

    public bool RemovePacket<T>()
    {
        return NetworkManager.PacketProcessor.RemoveCallback<T>(IsServer);
    }
}