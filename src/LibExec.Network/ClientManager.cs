using LiteNetLib;

namespace LibExec.Network;

public sealed class ClientManager : ManagerBase
{
    public ClientManager()
    {
        NetworkManager.RegisterPacket<SpawnNetworkObjectPacket>(OnSpawnNetworkObject);
        NetworkManager.RegisterPacket<DestroyNetworkObjectPacket>(OnDestroyNetworkObject);
    }

    public string Address { get; internal set; } = NetworkManager.LocalAddress;
    public NetPeer Peer => Manager.FirstPeer;

    internal override void Start()
    {
        if (Manager.IsRunning) return;
        ConnectionState = ConnectionState.Starting;

        if (!Manager.Start())
        {
            ConnectionState = ConnectionState.Stopped;
            return;
        }

        Manager.Connect(Address, NetworkManager.Port, NetworkManager.Key);

        Task.Run(PollEventsAsync);
    }

    protected override void OnPeerConnected(NetPeer peer)
    {
        ConnectionState = ConnectionState.Started;
    }

    protected override void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        Stop();
    }

    private void OnSpawnNetworkObject(SpawnNetworkObjectPacket packet)
    {
        var instance = NetworkManager.CreateNetworkObject(packet.Type);
        instance.Id = packet.Id;
        instance.OwnerId = packet.OwnerId;
        if (instance.IsOwner)
        {
            instance.Owner = Manager.FirstPeer;
        }

        NetworkManager.AddNetworkObject(instance);
    }

    private void OnDestroyNetworkObject(DestroyNetworkObjectPacket packet)
    {
        var instance = NetworkManager.NetworkObjects[packet.Id];
        NetworkManager.RemoveNetworkObject(instance);
    }

    internal bool IsLocalPeer(NetPeer peer)
    {
        return IsLocalPeerId(peer.Id);
    }

    internal bool IsLocalPeerId(int peerId)
    {
        return IsRunning && Manager.FirstPeer.RemoteId == peerId;
    }
}