using LiteNetLib;

namespace LibExec.Network;

public sealed class ServerManager : ManagerBase
{
    private uint _nextId;

    internal override void Start()
    {
        if (Manager.IsRunning) return;
        ConnectionState = ConnectionState.Starting;

        if (!Manager.Start(NetworkManager.Port))
        {
            ConnectionState = ConnectionState.Stopped;
            return;
        }

        ConnectionState = ConnectionState.Started;

        Task.Run(PollEventsAsync);
    }

    internal override void Stop()
    {
        if (!Manager.IsRunning) return;

        base.Stop();

        _nextId = 0;
    }

    protected override void OnConnectionRequest(ConnectionRequest request)
    {
        request.AcceptIfKey(NetworkManager.Key);
    }

    protected override void OnPeerConnected(NetPeer peer)
    {
        if (NetworkManager.PlayerType != null)
        {
            var instance = NetworkManager.CreateNetworkObject(NetworkManager.PlayerType);
            instance.Id = _nextId++;
            instance.Owner = peer;
            NetworkObjects.Add(instance.Id, instance);

            SpawnToAll(instance, peer);
        }

        foreach (var networkObject in NetworkObjects.Values)
        {
            Spawn(networkObject, peer);
        }
    }

    protected override void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        var res = NetworkObjects.FirstOrDefault(x => x.Value.Owner == peer).Value;
        if (res == null) return;
        Destroy(res, peer);
    }

    private void SpawnToAll(NetworkObject networkObject, NetPeer? excludePeer = null)
    {
        foreach (var peer in Manager.ConnectedPeerList.Where(peer => peer != excludePeer))
        {
            Spawn(networkObject, peer);
        }
    }

    private void Spawn(NetworkObject networkObject, NetPeer peer)
    {
        var packet = new SpawnNetworkObjectPacket
        {
            Type = networkObject.GetType(),
            Id = networkObject.Id,
            IsOwner = peer == networkObject.Owner
        };

        peer.Send(packet.GetData(), DeliveryMethod.ReliableOrdered);
    }

    internal void SpawnWithInit(NetworkObject networkObject, NetPeer? peer = null)
    {
        networkObject.Id = _nextId++;
        networkObject.Owner = peer;
        NetworkObjects.Add(networkObject.Id, networkObject);
        SpawnToAll(networkObject);
    }

    internal void Destroy(NetworkObject networkObject, NetPeer? exclude = null)
    {
        NetworkObjects.Remove(networkObject.Id);

        var packet = new DestroyNetworkObjectPacket { Id = networkObject.Id };
        Manager.SendToAll(packet.GetData(), DeliveryMethod.ReliableOrdered, exclude);
    }
}