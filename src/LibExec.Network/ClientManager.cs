using LiteNetLib;

namespace LibExec.Network;

public sealed class ClientManager : ManagerBase
{
    public ClientManager()
    {
        NetworkManager.PacketProcessor.RegisterCallback<SpawnNetworkObjectPacket>(OnSpawnNetworkObject,
            Channel.Spawn, IsServer);
        NetworkManager.PacketProcessor.RegisterCallback<DestroyNetworkObjectPacket>(OnDestroyNetworkObject,
            Channel.Destroy, IsServer);
        NetworkManager.PacketProcessor.RegisterCallback<UpdateFieldPacket>(OnUpdateField, Channel.ReplicateField,
            IsServer);
        NetworkManager.PacketProcessor.RegisterCallback<UpdatePropertyPacket>(OnUpdateProperty,
            Channel.ReplicateProperty, IsServer);
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
        ConnectionState = ConnectionState.Stopped;
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

        foreach (var field in packet.Fields)
        {
            NetworkManager.FieldInfos[field.Id].SetValue(instance, field.Value);
        }

        foreach (var property in packet.Properties)
        {
            NetworkManager.PropertyInfos[property.Id].SetValue(instance, property.Value);
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

    private static void OnUpdateField(UpdateFieldPacket packet)
    {
        var instance = NetworkManager.NetworkObjects[packet.Field.NetworkObjectId];
        NetworkManager.FieldInfos[packet.Field.Id].SetValue(instance, packet.Field.Value);
    }

    private static void OnUpdateProperty(UpdatePropertyPacket packet)
    {
        var instance = NetworkManager.NetworkObjects[packet.Property.NetworkObjectId];
        NetworkManager.PropertyInfos[packet.Property.Id].SetValue(instance, packet.Property.Value);
    }
}