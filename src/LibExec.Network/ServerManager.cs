using LiteNetLib;

namespace LibExec.Network;

public sealed class ServerManager : ManagerBase
{
    private const int FirstId = 1;

    private uint _nextId = FirstId;

    public NetConnection[] Connections { get; private set; } = null!;

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
    }

    protected override void OnConnectionRequest(ConnectionRequest request)
    {
        request.AcceptIfKey(NetworkManager.Key);
    }

    protected override void OnPeerConnected(NetPeer peer)
    {
        UpdateConnections();

        if (!peer.IsLocal())
        {
            foreach (var networkObject in NetworkManager.NetworkObjects.Values)
            {
                Spawn(networkObject, peer);
            }
        }

        if (Reflection.PlayerType == null) return;

        var instance = NetworkManager.CreateNetworkObject(Reflection.PlayerType);
        InitNetworkObject(instance, peer);

        SpawnToAll(instance);
        NetworkManager.AddNetworkObject(instance);
    }

    protected override void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        UpdateConnections();

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
        var networkObjectType = networkObject.GetType();
        var networkObjectId = networkObject.Id;

        var fields = NetworkManager.FieldInfosByType[networkObjectType]
            .Where(x => x.Attribute.Condition != NetworkObject.OwnerOnly || networkObject.OwnerId == peer.Id)
            .Select(x => new NetField(networkObjectId, x.Id, x.GetValue(networkObject))).Where(x => x.Value != null!)
            .ToArray();

        var packet = new SpawnNetworkObjectPacket
        {
            Type = networkObjectType,
            Id = networkObjectId,
            OwnerId = networkObject.OwnerId,
            Fields = fields
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
        networkObject.Owner = NetConnection.Create(owner);
    }

    public void SendPacketToAll<T>(T packet, DeliveryMethod deliveryMethod = DeliveryMethod.ReliableOrdered,
        NetConnection? excludeConnection = null, bool excludeLocalConnection = false) where T : class, new()
    {
        foreach (var peer in Manager.ConnectedPeerList.Where(x => x != excludeConnection?.Peer))
        {
            if (excludeLocalConnection && peer.IsLocal())
            {
                continue;
            }

            peer.SendPacket(packet, deliveryMethod);
        }
    }

    private void UpdateConnections()
    {
        Connections = Manager.ConnectedPeerList.Select(x => new NetConnection(x)).ToArray();
    }
}