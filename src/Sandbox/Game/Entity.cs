using LibExec.Network;

namespace Sandbox.Game;

public sealed class Entity : NetworkObject
{
    public int Value { get; } = 25;
}