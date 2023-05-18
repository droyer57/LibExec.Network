using System.Reflection;

namespace LibExec.Network;

public abstract class NetworkObject
{
    public uint Id { get; internal set; }
    internal int OwnerId { get; set; }
    public bool IsOwner => ClientManager.IsLocalPeerId(OwnerId);
    public NetConnection? Owner { get; internal set; }

    private static NetworkManager NetworkManager => NetworkManager.Instance;
    private static ClientManager ClientManager => NetworkManager.ClientManager;
    private static ServerManager ServerManager => NetworkManager.ServerManager;

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

    // ReSharper disable once UnusedMember.Global
    protected static bool ServerPatch(NetworkObject instance, MethodInfo originalMethod, object[] args)
    {
        var networkManager = NetworkManager.Instance;

        if (networkManager.IsClientOnly)
        {
            var packet = NetworkManager.GetInvokeMethodPacket(originalMethod, instance, args);
            networkManager.ClientManager.SendPacket(packet);
        }

        return networkManager.IsServer;
    }

    // ReSharper disable once UnusedMember.Global
    protected static bool MulticastPatch(NetworkObject instance, MethodInfo originalMethod, object[] args)
    {
        var networkManager = NetworkManager.Instance;

        if (networkManager.IsClientOnly)
        {
            return true;
        }

        var packet = NetworkManager.GetInvokeMethodPacket(originalMethod, instance, args);
        networkManager.ServerManager.SendPacketToAll(packet, excludeLocalConnection: true);
        return true;
    }

    // ReSharper disable once UnusedMember.Global
    protected static bool ClientPatch(NetworkObject instance, MethodInfo originalMethod, object[] args)
    {
        var networkManager = NetworkManager.Instance;

        if (networkManager.IsServer && instance.Owner is { IsLocal: false })
        {
            var packet = NetworkManager.GetInvokeMethodPacket(originalMethod, instance, args);
            instance.Owner.SendPacket(packet);
            return false;
        }

        return true;
    }
}