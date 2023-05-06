using LibExec.Network.SourceGenerators.Models;

namespace LibExec.Network.SourceGenerators.Builder;

internal sealed class PacketClassBuilder : BuilderBase
{
    private const string PacketFullClassName = "global::LibExec.Network.Packet";

    internal void GenerateUsingDirectives()
    {
        AppendLine("using System.Text.Json.Serialization;");
        AppendLine();
    }

    internal void GenerateNamespace(PacketClassToGenerate source)
    {
        AppendLine($"namespace {source.NamespaceName};");
        AppendLine();
    }

    internal void GenerateClass(PacketClassToGenerate source)
    {
        AppendLine($"partial class {source.ClassName} : {PacketFullClassName}");
        AppendLine("{");
        IncreaseIndent();
    }

    internal void GenerateProperties(PacketClassToGenerate source)
    {
        for (var i = 0; i < source.Properties.Count; i++)
        {
            var property = source.Properties[i];

            if (property.JsonIgnore)
            {
                AppendLine("[JsonIgnore]");
            }

            AppendLine($"public {property.TypeName} {property.PropertyName}");
            AppendLine("{");
            IncreaseIndent();
            AppendLine($"get => {property.BackingName};");
            AppendLine($"init => {property.BackingName} = value;");
            DecreaseIndent();
            AppendLine("}");

            if (i < source.Properties.Count - 1)
            {
                AppendLine();
            }
        }
    }
}