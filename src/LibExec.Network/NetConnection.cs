using LiteNetLib;

namespace LibExec.Network;

public sealed class NetConnection
{
    internal NetConnection(NetPeer peer, bool asServer)
    {
        Peer = peer;
        Id = asServer ? Peer.Id : Peer.RemoteId;
        IsLocal = NetworkManager.ClientManager.IsLocalPeerId(Id);
    }

    private static NetworkManager NetworkManager => NetworkManager.Instance;

    internal NetPeer Peer { get; }

    public int Id { get; }
    public bool IsLocal { get; }

    public void SendPacket<T>(T packet, DeliveryMethod deliveryMethod = DeliveryMethod.ReliableOrdered,
        bool excludeLocalConnection = false) where T : class, new()
    {
        Peer.SendPacket(packet, deliveryMethod, excludeLocalConnection);
    }

    public void Disconnect()
    {
        Peer.Disconnect();
    }
}