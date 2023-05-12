using LibExec.Network;

namespace Sandbox.Game;

[Packet]
public sealed class TestPacket
{
    public string Text { get; init; } = null!;
}