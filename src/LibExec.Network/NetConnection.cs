using LiteNetLib;

namespace LibExec.Network;

public sealed class NetConnection : IEquatable<NetConnection>
{
    internal NetConnection(NetPeer peer)
    {
        Peer = peer;
        Id = NetworkManager.IsServer ? Peer.Id : Peer.RemoteId;
        IsLocal = NetworkManager.ClientManager.IsLocalPeerId(Id);
    }

    private static NetworkManager NetworkManager => NetworkManager.Instance;

    internal NetPeer Peer { get; }

    public int Id { get; }
    public bool IsLocal { get; }

    public bool Equals(NetConnection? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Id == other.Id;
    }

    internal static NetConnection? Create(NetPeer? peer)
    {
        return peer == null ? null : new NetConnection(peer);
    }

    public void SendPacket<T>(T packet, DeliveryMethod deliveryMethod = DeliveryMethod.ReliableOrdered,
        bool excludeLocalConnection = false) where T : class, new()
    {
        Peer.SendPacket(packet, deliveryMethod, excludeLocalConnection);
    }

    public void Disconnect()
    {
        Peer.Disconnect();
    }

    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || (obj is NetConnection other && Equals(other));
    }

    public override int GetHashCode()
    {
        return Id;
    }
}