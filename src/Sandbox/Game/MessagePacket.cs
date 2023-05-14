using LibExec.Network;

namespace Sandbox.Game;

[Packet]
public sealed class MessagePacket
{
    public string Text { get; init; } = null!;
    public string SenderId { get; init; } = null!;
}