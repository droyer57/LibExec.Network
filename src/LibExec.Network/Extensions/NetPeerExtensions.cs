using LiteNetLib;
using LiteNetLib.Utils;

namespace LibExec.Network;

internal static class NetPeerExtensions
{
    private static NetworkManager NetworkManager => NetworkManager.Instance;

    public static void SendPacket<T>(this NetPeer peer, T packet,
        DeliveryMethod deliveryMethod = DeliveryMethod.ReliableOrdered) where T : class, new()
    {
        var writer = new NetDataWriter();
        NetworkManager.Instance.PacketProcessor.Write(writer, packet);
        peer.Send(writer, deliveryMethod);
    }

    public static bool IsLocal(this NetPeer peer)
    {
        return NetworkManager.ClientManager.IsLocalPeer(peer);
    }
}