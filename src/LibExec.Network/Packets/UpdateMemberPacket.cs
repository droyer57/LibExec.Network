namespace LibExec.Network;

[Packet]
public sealed class UpdateMemberPacket
{
    public UpdateMemberPacket(NetMember member)
    {
        Member = member;
    }

    public UpdateMemberPacket()
    {
    }

    public NetMember Member { get; private set; }
}