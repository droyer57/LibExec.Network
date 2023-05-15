namespace LibExec.Network;

[Packet]
internal sealed class InvokeMethodPacket
{
    public InvokeMethodPacket(NetMethod method)
    {
        Method = method;
    }

    public InvokeMethodPacket()
    {
    }

    public NetMethod Method { get; private set; }
}