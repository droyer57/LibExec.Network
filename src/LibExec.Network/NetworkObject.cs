using LiteNetLib;

namespace LibExec.Network;

public class NetworkObject
{
    public uint Id { get; internal set; }
    internal NetPeer? Owner { get; set; }
    internal bool IsOwner { get; set; }

    internal NetworkManager NetworkManager => NetworkManager.Instance;
    internal ClientManager ClientManager => NetworkManager.ClientManager;
    internal ServerManager ServerManager => NetworkManager.ServerManager;

    public void Spawn(NetPeer? owner = null)
    {
        NetworkManager.EnsureMethodCalledByServer();
        ServerManager.SpawnWithInit(this, owner);
    }

    public void Destroy()
    {
        NetworkManager.EnsureMethodCalledByServer();
        ServerManager.Destroy(this);
    }

    public bool IsValid()
    {
        if (ServerManager.IsRunning)
        {
            return ServerManager.NetworkObjects.ContainsKey(Id);
        }

        return ClientManager.NetworkObjects.ContainsKey(Id);
    }

    public static T? UpdateRef<T>(T networkObject) where T : NetworkObject?
    {
        return networkObject?.IsValid() == true ? networkObject : null;
    }

    public static void UpdateRef<T>(ref T? networkObject) where T : NetworkObject
    {
        if (networkObject?.IsValid() == false)
        {
            networkObject = null;
        }
    }
}