namespace LibExec.Network;

public abstract class NetworkObject
{
    public uint Id { get; internal set; }
    internal int OwnerId { get; set; }
    public bool IsOwner => ClientManager.IsLocalPeerId(OwnerId);
    public NetConnection? Owner { get; internal set; }

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
}