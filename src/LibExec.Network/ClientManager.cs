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

        NetworkManager.AddNetworkObject(instance);
    }

    private void OnDestroyNetworkObject(DestroyNetworkObjectPacket packet)
    {
        var instance = NetworkManager.NetworkObjects[packet.Id];
        NetworkManager.RemoveNetworkObject(instance);
    }

    public bool IsLocalPeer(NetPeer peer)
    {
        return IsLocalPeerId(peer.Id);
    }

    public bool IsLocalPeerId(int peerId)
    {
        return IsRunning && Manager.FirstPeer.RemoteId == peerId;
    }
}