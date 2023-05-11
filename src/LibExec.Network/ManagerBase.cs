using LiteNetLib;

namespace LibExec.Network;

public abstract class ManagerBase
{
    private readonly EventBasedNetListener _listener = new();
    internal readonly NetManager Manager; // todo: make it protected
    private ConnectionState _connectionState;

    protected ManagerBase()
    {
        Manager = new NetManager(_listener);
        _listener.ConnectionRequestEvent += OnConnectionRequest;
        _listener.PeerConnectedEvent += OnPeerConnected;
        _listener.PeerDisconnectedEvent += OnPeerDisconnected;
        _listener.NetworkReceiveEvent += OnNetworkReceive;
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
        var type = NetworkManager.PacketTypes.Get(reader.GetByte());
        var packet = NetworkManager.CreatePacket(type);
        packet.DeserializeInternal(reader);
        NetworkManager.PacketCallbacks[type].Invoke(packet);
    }
}