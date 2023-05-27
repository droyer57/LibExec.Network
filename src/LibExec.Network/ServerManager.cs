using LiteNetLib;

namespace LibExec.Network;

public sealed class ServerManager : ManagerBase
{
    private const int FirstId = 1;

    private uint _nextId = FirstId;

    public Dictionary<int, NetConnection> Connections { get; } = new();

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
    }

    internal override void Stop()
    {
        if (!Manager.IsRunning) return;

        base.Stop();

        _nextId = FirstId;
        ConnectionState = ConnectionState.Stopped;
        Connections.Clear();
    }

    protected override void OnConnectionRequest(ConnectionRequest request)
    {
        request.AcceptIfKey(NetworkManager.Key);
    }

    protected override void OnPeerConnected(NetPeer peer)
    {
        Connections[peer.Id] = new NetConnection(peer, AsServer);

        if (!peer.IsLocal())
        {
            foreach (var networkObject in NetworkManager.NetworkObjects.Values)
            {
                Spawn(networkObject, peer);
            }
        }

        if (NetworkManager.PlayerClassId == 0) return;

        var instance = NetworkManager.CreateNetworkObject(NetworkManager.PlayerClassId);
        InitNetworkObject(instance, peer);

        SpawnToAll(instance);
        NetworkManager.AddNetworkObject(instance);
    }

    protected override void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        Connections.Remove(peer.Id);

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

    private static void Spawn(NetworkObject networkObject, NetPeer peer)
    {
        var networkObjectId = networkObject.Id;

        var members = NetworkManager.MemberInfosByClassId.GetValueOrDefault(networkObject.ClassId)?.Where(x =>
                x.Attribute.Condition != NetworkObject.OwnerOnly || networkObject.OwnerId == peer.Id)
            .Select(x => new NetMember(networkObjectId, x.Id, x.GetValue(networkObject)))
            .Where(x => x.Value != null!).ToArray();

        var packet = new SpawnNetworkObjectPacket
        {
            ClassId = networkObject.ClassId,
            Id = networkObjectId,
            OwnerId = networkObject.OwnerId,
            Members = members ?? Array.Empty<NetMember>()
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
        return Manager.ConnectedPeerList.Where(x => x != excludePeer).Where(peer => !peer.IsLocal());
    }

    private void InitNetworkObject(NetworkObject networkObject, NetPeer? owner)
    {
        networkObject.Id = _nextId++;
        networkObject.OwnerId = owner?.Id ?? -1;
        if (owner != null)
        {
            networkObject.Owner = Connections[owner.Id];
        }
    }

    public void SendPacketToAll<T>(T packet, DeliveryMethod deliveryMethod = DeliveryMethod.ReliableOrdered,
        NetConnection? excludeConnection = null, bool excludeLocalConnection = false) where T : class, new()
    {
        foreach (var connection in Connections.Values.Where(x => x != excludeConnection))
        {
            if (excludeLocalConnection && connection.IsLocal)
            {
                continue;
            }

            connection.SendPacket(packet, deliveryMethod);
        }
    }
}