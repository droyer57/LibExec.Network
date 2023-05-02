using System.Collections.Generic;

namespace LibExec.Network.SourceGenerators.Models;

internal sealed class PacketClassToGenerate
{
    public string ClassName { get; init; } = null!;
    public string NamespaceName { get; init; } = null!;
    public List<PacketProperty> Properties { get; init; } = null!;
}