using LibExec.Network;

namespace Sandbox.Game;

public sealed class Entity : NetworkObject
{
    public int Value { get; private set; }

    [Server]
    public void ChangeValueServer()
    {
        Value++;
    }
}