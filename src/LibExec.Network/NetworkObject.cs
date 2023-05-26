namespace LibExec.Network;

public abstract class NetworkObject
{
    public const int OwnerOnly = 1;
    public const int SkipOwner = 2;

    public uint Id { get; internal set; }
    internal int OwnerId { get; set; }
    public bool IsOwner => ClientManager.IsLocalPeerId(OwnerId);
    public NetConnection? Owner { get; internal set; }

#nullable disable
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    internal Type Type { get; set; }
#nullable restore

    protected static NetworkManager NetworkManager => NetworkManager.Instance;
    protected static ClientManager ClientManager => NetworkManager.ClientManager;
    protected static ServerManager ServerManager => NetworkManager.ServerManager;

    public bool IsValid => NetworkManager.NetworkObjects.ContainsKey(Id);

    public void Spawn(NetConnection? owner = null)
    {
        NetworkManager.EnsureMethodIsCalledByServer();
        ServerManager.SpawnWithInit(this, owner?.Peer);
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
    private void UpdateMember(object newValue, ushort memberId)
    {
        var memberInfo = NetworkManager.MemberInfos[memberId];
        var oldValue = memberInfo.SetValue(this, newValue);

        if (!NetworkManager.IsServer || !IsValid || newValue == oldValue) return;

        var packet = new UpdateMemberPacket(new NetMember(Id, memberId, newValue));
        var attribute = memberInfo.Attribute;

        if (attribute.Condition == OwnerOnly)
        {
            Owner!.SendPacket(packet, excludeLocalConnection: true);
        }
        else
        {
            var excludeConnection = attribute.Condition == SkipOwner ? Owner : null;
            ServerManager.SendPacketToAll(packet, excludeLocalConnection: true, excludeConnection: excludeConnection);
        }
    }
}