using LibExec.Network;

namespace Sandbox.Game;

public sealed class Entity : NetworkObject
{
    [Replicate] public int Value { get; set; }

    [Server]
    public void SetValueServer()
    {
        Value++;
        // SetValueMulticast(5);
    }

    [Multicast]
    private void SetValueMulticast(int delta)
    {
        // Value += delta;
    }
}