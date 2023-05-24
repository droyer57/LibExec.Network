namespace LibExec.Network;

[Packet]
public sealed class UpdatePropertyPacket
{
    public UpdatePropertyPacket(NetProperty property)
    {
        Property = property;
    }

    public UpdatePropertyPacket()
    {
    }

    public NetProperty Property { get; private set; }
}