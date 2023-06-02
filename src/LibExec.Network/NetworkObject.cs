namespace LibExec.Network;

public abstract class NetworkObject
{
    public const int OwnerOnly = 1;
    public const int SkipOwner = 2;

    private UpdateMemberData? _updateMemberData;

    public uint Id { get; internal set; }
    internal int OwnerId { get; set; }
    public bool IsOwner => ClientManager.IsLocalPeerId(OwnerId);
    public NetConnection? Owner { get; internal set; }
    internal ushort ClassId { get; set; } // set by the code generator

    protected static NetworkManager NetworkManager => NetworkManager.Instance;
    protected static ClientManager ClientManager => NetworkManager.ClientManager;
    protected static ServerManager ServerManager => NetworkManager.ServerManager;

    public bool IsValid => NetworkManager.NetworkObjects.ContainsKey(Id);

    public void Spawn(NetConnection? owner = null)
    {
        NetworkManager.EnsureMethodIsCalledByServer();
        ServerManager.SpawnWithInit(this, owner?.Peer);

        if (_updateMemberData != null)
        {
            SendMember(_updateMemberData);
        }
    }

    public void Destroy()
    {
        NetworkManager.EnsureMethodIsCalledByServer();
        ServerManager.Destroy(this);
    }

    public virtual void OnSpawn()
    {
    }

    public virtual void OnDestroy()
    {
    }

    public static T? UpdateRef<T>(T networkObject) where T : NetworkObject?
    {
        return networkObject?.IsValid == true ? networkObject : null;
    }

    public static void UpdateRef<T>(ref T? networkObject) where T : NetworkObject
    {
        if (networkObject?.IsValid == false)
        {
            networkObject = null;
        }
    }

    // ReSharper disable once UnusedMember.Local
    private void UpdateMember(object newValue, ushort memberId) // called by the code generator
    {
        var memberInfo = NetworkManager.MemberInfos[memberId];
        memberInfo.SetValue(this, newValue, out var oldValue);

        if (!NetworkManager.IsServer || newValue == oldValue) return;

        if (newValue is NetworkObject { IsValid: false } networkObject)
        {
            networkObject._updateMemberData = GetMemberData(memberId, newValue);
            return;
        }

        if (!IsValid) return;

        SendMember(GetMemberData(memberId, newValue));
    }

    private void SendMember(UpdateMemberData data)
    {
        var packet = new UpdateMemberPacket(new NetMember(data.InstanceId, data.MemberId, data.Value));

        if (data.Attribute.Condition == OwnerOnly)
        {
            Owner!.SendPacket(packet, excludeLocalConnection: true);
        }
        else
        {
            var excludeConnection = data.Attribute.Condition == SkipOwner ? Owner : null;
            ServerManager.SendPacketToAll(packet, excludeLocalConnection: true, excludeConnection: excludeConnection);
        }
    }

    private UpdateMemberData GetMemberData(ushort memberId, object newValue)
    {
        var memberInfo = NetworkManager.MemberInfos[memberId];
        return new UpdateMemberData(Id, memberId, newValue, memberInfo.Attribute);
    }
}