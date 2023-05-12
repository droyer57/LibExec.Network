using System.Collections.Immutable;
using System.Linq;
using LibExec.Network.SourceGenerators.Models;

namespace LibExec.Network.SourceGenerators.Builder;

internal sealed class NetworkInitClassBuilder : BuilderBase
{
    internal void GenerateUsingDirectives()
    {
        AppendLine("using LibExec.Network;");
        AppendLine();
    }

    internal void GenerateClass()
    {
        AppendLine("// ReSharper disable once UnusedType.Global");
        AppendLine("internal sealed class InternalNetworkInit");
        AppendLine("{");
        IncreaseIndent();
    }

    internal void GenerateConstructor(ImmutableArray<NetworkInitDataToGenerate> source)
    {
        var networkObjects = source.Where(x => x.NetworkObjectName != null).Select(x => x.NetworkObjectName);

        AppendLine("private InternalNetworkInit()");
        AppendLine("{");
        IncreaseIndent();

        foreach (var item in networkObjects)
        {
            AppendLine($"NetworkManager.Instance.RegisterNetworkObject<{item}>();");
        }

        DecreaseIndent();
        AppendLine("}");
    }
}