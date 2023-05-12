using LiteNetLib;

namespace LibExec.Network;

public abstract class NetworkObject
{
    public uint Id { get; internal set; }
    internal int OwnerId { get; set; }
    public bool IsOwner => ClientManager.IsLocalPeerId(OwnerId);
    public NetPeer? Owner { get; internal set; }

    private NetworkManager NetworkManager => NetworkManager.Instance;
    private ClientManager ClientManager => NetworkManager.ClientManager;
    private ServerManager ServerManager => NetworkManager.ServerManager;

    public bool IsValid => NetworkManager.NetworkObjects.ContainsKey(Id);

    public void Spawn(NetPeer? owner = null)
    {
        NetworkManager.EnsureMethodIsCalledByServer();
        ServerManager.SpawnWithInit(this, owner);
    }

    public void Destroy()
    {
        NetworkManager.EnsureMethodIsCalledByServer();
        ServerManager.Destroy(this);
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