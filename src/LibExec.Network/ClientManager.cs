using LiteNetLib;

namespace LibExec.Network;

public sealed class ClientManager : ManagerBase
{
    public ClientManager()
    {
        RegisterPacket<SpawnNetworkObjectPacket>(OnSpawnNetworkObject);
        RegisterPacket<DestroyNetworkObjectPacket>(OnDestroyNetworkObject);
        RegisterPacket<UpdateFieldPacket>(NetworkManager.OnUpdateField);
    }

    public string Address { get; internal set; } = NetworkManager.LocalAddress;
    public NetConnection Connection { get; private set; } = null!;

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
        Connection = new NetConnection(peer);
        ConnectionState = ConnectionState.Started;
    }

    protected override void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        Stop();
        Connection = null!;
    }

    private void OnSpawnNetworkObject(SpawnNetworkObjectPacket packet)
    {
        var instance = NetworkManager.CreateNetworkObject(packet.Type);
        instance.Id = packet.Id;
        instance.OwnerId = packet.OwnerId;
        if (instance.IsOwner)
        {
            instance.Owner = NetConnection.Create(Manager.FirstPeer);
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
        return IsLocalPeerId(NetworkManager.IsServer ? peer.Id : peer.RemoteId);
    }

    internal bool IsLocalPeerId(int peerId)
    {
        return IsRunning && Manager.FirstPeer.RemoteId == peerId;
    }

    public void SendPacket<T>(T packet, DeliveryMethod deliveryMethod = DeliveryMethod.ReliableOrdered)
        where T : class, new()
    {
        Connection.SendPacket(packet, deliveryMethod);
    }
}