using LibExec.Network;

namespace Sandbox.Game;

public sealed class Entity : NetworkObject
{
    public int Value { get; private set; }

    [Server]
    public void SetValueServer()
    {
        SetValueMulticast(5);
    }

    [Multicast]
    private void SetValueMulticast(int delta)
    {
        Value += delta;
    }
}