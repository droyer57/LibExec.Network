using System.Collections.Immutable;
using System.Text;
using LibExec.Network.SourceGenerators.Builder;
using LibExec.Network.SourceGenerators.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace LibExec.Network.SourceGenerators.Generators;

[Generator]
internal sealed class NetworkInitGenerator : IIncrementalGenerator
{
    private const string NetworkObjectClassName = "NetworkObject";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var provider = context.SyntaxProvider.CreateSyntaxProvider(
                (s, _) => IsSyntaxTarget(s),
                (s, _) => GetSemanticTarget(s))
            .Where(x => x is not null);

        var compilation = context.CompilationProvider.Combine(provider.Collect());

        context.RegisterSourceOutput(compilation, (spc, symbol) => Execute(spc, symbol.Right!));
    }

    private static bool IsSyntaxTarget(SyntaxNode node)
    {
        return node is ClassDeclarationSyntax;
    }

    private static NetworkInitDataToGenerate? GetSemanticTarget(GeneratorSyntaxContext context)
    {
        var classDeclarationSyntax = (ClassDeclarationSyntax)context.Node;
        var classSymbol = context.SemanticModel.GetDeclaredSymbol(classDeclarationSyntax);
        if (classSymbol == null) return null;

        if (classSymbol.BaseType?.Name == NetworkObjectClassName)
        {
            return new NetworkInitDataToGenerate
                { NetworkObjectName = $"{classSymbol.ContainingNamespace}.{classSymbol.Name}" };
        }

        return null;
    }

    private static void Execute(SourceProductionContext context, ImmutableArray<NetworkInitDataToGenerate> source)
    {
        const string fileName = "InternalNetworkInit.g.cs";

        var builder = new NetworkInitClassBuilder();
        builder.GenerateUsingDirectives();
        builder.GenerateClass();
        builder.GenerateConstructor(source);

        while (builder.DecreaseIndent())
        {
            builder.AppendLine("}");
        }

        var sourceText = SourceText.From(builder.ToString(), Encoding.UTF8);
        context.AddSource(fileName, sourceText);
    }
}