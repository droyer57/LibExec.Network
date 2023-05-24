using LiteNetLib;
using LiteNetLib.Utils;

namespace LibExec.Network;

internal static class NetPeerExtensions
{
    private static NetworkManager NetworkManager => NetworkManager.Instance;

    public static void SendPacket<T>(this NetPeer peer, T packet,
        DeliveryMethod deliveryMethod = DeliveryMethod.ReliableOrdered, bool excludeLocalConnection = false)
        where T : class, new()
    {
        if (excludeLocalConnection && peer.IsLocal())
        {
            return;
        }

        var writer = new NetDataWriter();
        var channel = NetworkManager.PacketProcessor.Write(writer, packet);
        peer.Send(writer, channel, deliveryMethod);
    }

    public static bool IsLocal(this NetPeer peer)
    {
        return NetworkManager.ClientManager.IsLocalPeer(peer);
    }
}