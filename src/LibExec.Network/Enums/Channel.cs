namespace LibExec.Network;

internal enum Channel : byte
{
    Default = 0,
    Spawn = 1,
    Destroy = 2,
    Rpc = 3,
    ReplicateField = 4,
    ReplicateProperty = 5
}