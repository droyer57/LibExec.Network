using LibExec.Network;

namespace Sandbox.Game;

public sealed class Entity : NetworkObject
{
    public Entity(int value)
    {
        Value = value;
    }

    [Replicate] public int Value { get; private set; }

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