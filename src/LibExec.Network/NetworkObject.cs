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
        // ServerManager.Spawn(this, owner);
    }
}