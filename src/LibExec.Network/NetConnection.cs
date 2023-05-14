using LiteNetLib;
using LiteNetLib.Utils;

namespace LibExec.Network;

public sealed class NetConnection
{
    internal NetConnection(NetPeer peer)
    {
        Peer = peer;
    }

    private static NetworkManager NetworkManager => NetworkManager.Instance;

    internal NetPeer Peer { get; }

    public int Id => NetworkManager.IsServer ? Peer.Id : Peer.RemoteId;
    public bool IsLocal => NetworkManager.ClientManager.IsLocalPeerId(Id);

    internal static NetConnection? Create(NetPeer? peer)
    {
        return peer == null ? null : new NetConnection(peer);
    }

    public void SendPacket<T>(T packet, DeliveryMethod deliveryMethod = DeliveryMethod.ReliableOrdered)
        where T : class, new()
    {
        var writer = new NetDataWriter();
        NetworkManager.PacketProcessor.Write(writer, packet);
        Peer.Send(writer, deliveryMethod);
    }
}