using LiteNetLib;

namespace LibExec.Network;

public sealed class ClientManager : ManagerBase
{
    public ClientManager()
    {
        NetworkManager.PacketProcessor.RegisterCallback<SpawnNetworkObjectPacket>(OnSpawnNetworkObject,
            Channel.Spawn, AsServer);
        NetworkManager.PacketProcessor.RegisterCallback<DestroyNetworkObjectPacket>(OnDestroyNetworkObject,
            Channel.Destroy, AsServer);
        NetworkManager.PacketProcessor.RegisterCallback<UpdateMemberPacket>(OnUpdateMember, Channel.ReplicateMember,
            AsServer);
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
        Connection = new NetConnection(peer, AsServer);
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
        var instance = NetworkManager.CreateNetworkObject(packet.ClassId);
        instance.Id = packet.Id;
        instance.OwnerId = packet.OwnerId;
        if (instance.IsOwner)
        {
            instance.Owner = Connection;
        }

        NetworkManager.AddNetworkObject(instance);

        foreach (var member in packet.Members)
        {
            NetworkManager.MemberInfos[member.Id].SetValue(instance, member.Value);
        }

        NetworkManager.InvokeNetworkObjectSpawn(instance);
    }

    private void OnDestroyNetworkObject(DestroyNetworkObjectPacket packet)
    {
        var instance = NetworkManager.NetworkObjects[packet.Id];
        NetworkManager.RemoveNetworkObject(instance);
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

    private static void OnUpdateMember(UpdateMemberPacket packet)
    {
        var instance = NetworkManager.NetworkObjects[packet.Member.NetworkObjectId];
        NetworkManager.MemberInfos[packet.Member.Id].SetValue(instance, packet.Member.Value);
    }

    internal void OnReceiveAllObjects(NetPacketReader reader)
    {
        var networkObjectsCount = reader.GetUShort();
        for (var i = 0; i < networkObjectsCount; i++)
        {
            var classId = reader.GetUShort();
            var id = reader.GetUInt();

            var instance = NetworkManager.CreateNetworkObject(classId);
            instance.Id = id;
            NetworkManager.AddNetworkObject(instance);
        }

        foreach (var obj in NetworkManager.NetworkObjects.Values)
        {
            var members = new NetMember[reader.GetUShort()];
            for (var i = 0; i < members.Length; i++)
            {
                members[i] = reader.Get<NetMember>();
                NetworkManager.MemberInfos[members[i].Id].SetValue(obj, members[i].Value);
            }

            NetworkManager.InvokeNetworkObjectSpawn(obj);
        }
    }
}