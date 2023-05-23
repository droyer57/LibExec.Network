using LibExec.Network;

namespace Sandbox.Game;

public sealed class Entity : NetworkObject
{
    [Replicate] public int _value;
    // public int Value { get; private set; }

    public int Value => _value; // todo: tmp

    [Server]
    public void SetValueServer()
    {
        _value++;
        // SetValueMulticast(5);
    }

    [Multicast]
    private void SetValueMulticast(int delta)
    {
        // Value += delta;
    }
}