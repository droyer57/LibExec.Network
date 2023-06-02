namespace LibExec.Network;

internal class UpdateMemberData
{
    public UpdateMemberData(uint instanceId, ushort memberId, object value, ReplicateAttribute attribute)
    {
        InstanceId = instanceId;
        MemberId = memberId;
        Value = value;
        Attribute = attribute;
    }

    public ushort MemberId { get; }
    public uint InstanceId { get; }
    public object Value { get; }
    public ReplicateAttribute Attribute { get; }
}