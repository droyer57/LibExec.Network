using System.Collections.Generic;
using System.Linq;
using System.Text;
using LibExec.Network.SourceGenerators.Builder;
using LibExec.Network.SourceGenerators.Extensions;
using LibExec.Network.SourceGenerators.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace LibExec.Network.SourceGenerators.Generators;

[Generator]
internal sealed class PacketClassGenerator : IIncrementalGenerator
{
    private const string PacketClassName = "Packet";
    private const string PacketPropertyAttributeName = "LibExec.Network.PacketPropertyAttribute";
    private const string JsonIgnoreArgumentName = "JsonIgnore";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var provider = context.SyntaxProvider.CreateSyntaxProvider(
                (s, _) => IsSyntaxTarget(s),
                (s, _) => GetSemanticTarget(s))
            .Where(x => x is not null);

        context.RegisterSourceOutput(provider, Execute!);
    }

    private static bool IsSyntaxTarget(SyntaxNode node)
    {
        return node is ClassDeclarationSyntax;
    }

    private static PacketClassToGenerate? GetSemanticTarget(GeneratorSyntaxContext context)
    {
        var classDeclarationSyntax = (ClassDeclarationSyntax)context.Node;
        var classSymbol = context.SemanticModel.GetDeclaredSymbol(classDeclarationSyntax);
        if (classSymbol == null) return null;

        if (classSymbol.BaseType?.Name != PacketClassName) return null;

        return new PacketClassToGenerate
        {
            ClassName = classSymbol.Name,
            NamespaceName = classSymbol.ContainingNamespace.ToDisplayString(),
            Properties = GetProperties(classSymbol)
        };
    }

    private static List<PacketProperty> GetProperties(INamedTypeSymbol symbol)
    {
        var packetProperties = new List<PacketProperty>();

        foreach (var member in symbol.GetMembers())
        {
            if (member is not IFieldSymbol fieldSymbol) continue;

            foreach (var attribute in fieldSymbol.GetAttributes())
            {
                if (attribute.AttributeClass?.ToDisplayString() != PacketPropertyAttributeName) continue;

                var jsonIgnoreType =
                    attribute.NamedArguments.FirstOrDefault(x => x.Key == JsonIgnoreArgumentName).Value;
                var jsonIgnore = jsonIgnoreType.Value as bool?;

                packetProperties.Add(new PacketProperty
                {
                    PropertyName = fieldSymbol.Name.ToPropertyName(),
                    TypeName = fieldSymbol.Type.Name,
                    BackingName = fieldSymbol.Name,
                    JsonIgnore = jsonIgnore ?? false
                });
            }
        }

        return packetProperties;
    }

    private static void Execute(SourceProductionContext context, PacketClassToGenerate source)
    {
        var fileName = $"{source.ClassName}.g.cs";

        var builder = new PacketClassBuilder();
        builder.GenerateUsingDirectives();
        builder.GenerateNamespace(source);
        builder.GenerateClass(source);
        builder.GenerateProperties(source);

        while (builder.DecreaseIndent())
        {
            builder.AppendLine("}");
        }

        var sourceText = SourceText.From(builder.ToString(), Encoding.UTF8);
        context.AddSource(fileName, sourceText);
    }
}