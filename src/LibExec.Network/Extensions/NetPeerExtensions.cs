using LiteNetLib;
using LiteNetLib.Utils;

namespace LibExec.Network;

public static class NetPeerExtensions
{
    public static void SendPacket<T>(this NetPeer peer, T packet,
        DeliveryMethod deliveryMethod = DeliveryMethod.ReliableOrdered) where T : class, new()
    {
        var writer = new NetDataWriter();
        NetworkManager.Instance.PacketProcessor.Write(writer, packet);
        peer.Send(writer, deliveryMethod);
    }

    public static void SendPacket<T>(this IEnumerable<NetPeer> peers, T packet, NetPeer? excludePeer = null)
        where T : class, new()
    {
        foreach (var peer in peers.Where(x => x != excludePeer))
        {
            peer.SendPacket(packet);
        }
    }
}