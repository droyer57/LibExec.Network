namespace LibExec.Network;

[Packet]
public sealed class UpdateFieldPacket
{
    public UpdateFieldPacket(NetField field)
    {
        Field = field;
    }

    public UpdateFieldPacket()
    {
    }

    public NetField Field { get; private set; }
}