using LiteNetLib;

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

        var writer = NetworkManager.PacketProcessor.Write(packet, out var channel);
        peer.Send(writer, channel, deliveryMethod);
    }

    public static bool IsLocal(this NetPeer peer)
    {
        return NetworkManager.ClientManager.IsLocalPeerId(peer.Id);
    }
}