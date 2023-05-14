using LiteNetLib;

namespace LibExec.Network;

public abstract class ManagerBase
{
    private readonly EventBasedNetListener _listener = new();
    protected readonly NetManager Manager;
    private ConnectionState _connectionState;

    protected ManagerBase()
    {
        Manager = new NetManager(_listener);
        _listener.ConnectionRequestEvent += OnConnectionRequest;
        _listener.PeerConnectedEvent += OnPeerConnected;
        _listener.PeerDisconnectedEvent += OnPeerDisconnected;
        _listener.NetworkReceiveEvent += OnNetworkReceive;

        PacketProcessor = new PacketProcessor();
        PacketProcessor.RegisterType<NetworkObjectType>();

        RegisterPacket<InvokeMethodPacket>(NetworkManager.OnInvokeMethod);
    }

    protected static NetworkManager NetworkManager => NetworkManager.Instance;

    internal PacketProcessor PacketProcessor { get; }

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

    public event Action<ConnectionState>? ConnectionStateChangedEvent;

    protected async Task PollEventsAsync()
    {
        while (Manager.IsRunning)
        {
            Manager.PollEvents();
            await Task.Delay(Manager.UpdateTime);
        }

        ConnectionState = ConnectionState.Stopped;
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
        PacketProcessor.ReadAllPackets(reader, new NetConnection(peer));
        NetworkManager.InvokeNetworkEvent();
        reader.Recycle();
    }

    public void RegisterPacket<T>(Action<T> callback) where T : class, new()
    {
        PacketProcessor.RegisterCallback(callback);
    }

    public void RegisterPacket<T>(Action<T, NetConnection> callback) where T : class, new()
    {
        PacketProcessor.RegisterCallback(callback);
    }

    public bool RemovePacket<T>()
    {
        return PacketProcessor.RemoveCallback<T>();
    }
}