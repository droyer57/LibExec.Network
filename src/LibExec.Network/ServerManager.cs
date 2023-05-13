using LiteNetLib;

namespace LibExec.Network;

public sealed class ServerManager : ManagerBase
{
    private uint _nextId;

    public NetPeer[] Peers { get; private set; } = null!;

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
        Peers = Manager.ConnectedPeerList.ToArray();

        if (!NetworkManager.ClientManager.IsLocalPeer(peer))
        {
            foreach (var networkObject in NetworkManager.NetworkObjects.Values)
            {
                Spawn(networkObject, peer);
            }
        }

        if (Reflection.PlayerType == null) return;

        var instance = NetworkManager.CreateNetworkObject(Reflection.PlayerType);
        InitNetworkObject(instance, peer);
        NetworkManager.AddNetworkObject(instance);

        SpawnToAll(instance);
    }

    protected override void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        Peers = Manager.ConnectedPeerList.ToArray();

        var data = NetworkManager.NetworkObjects.Where(x => x.Value.OwnerId == peer.Id);
        foreach (var item in data)
        {
            Destroy(item.Value, peer);
        }
    }

    private void SpawnToAll(NetworkObject networkObject, NetPeer? excludePeer = null)
    {
        foreach (var peer in GetPeers(excludePeer))
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
            OwnerId = networkObject.OwnerId
        };

        peer.SendPacket(packet);
    }

    internal void SpawnWithInit(NetworkObject networkObject, NetPeer? owner)
    {
        InitNetworkObject(networkObject, owner);
        NetworkManager.AddNetworkObject(networkObject);
        SpawnToAll(networkObject);
    }

    internal void Destroy(NetworkObject networkObject, NetPeer? excludePeer = null)
    {
        if (!networkObject.IsValid) return;

        NetworkManager.RemoveNetworkObject(networkObject);

        var packet = new DestroyNetworkObjectPacket { Id = networkObject.Id };

        foreach (var peer in GetPeers(excludePeer))
        {
            peer.SendPacket(packet);
        }
    }

    private IEnumerable<NetPeer> GetPeers(NetPeer? excludePeer = null)
    {
        foreach (var peer in Manager.ConnectedPeerList.Where(x => x != excludePeer))
        {
            if (NetworkManager.ClientManager.IsLocalPeer(peer)) continue;

            yield return peer;
        }
    }

    private void InitNetworkObject(NetworkObject networkObject, NetPeer? owner)
    {
        networkObject.Id = _nextId++;
        networkObject.OwnerId = owner?.Id ?? -1;
        networkObject.Owner = owner;
    }
}