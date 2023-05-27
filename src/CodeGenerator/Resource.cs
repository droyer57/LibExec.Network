using System.Linq;
using Mono.Cecil;
using static CodeGenerator.Constants;

namespace CodeGenerator;

internal sealed class Resource
{
    public Resource(ModuleDefinition libModule, ModuleDefinition module)
    {
        NetworkObjectTypes = module.Types.Where(x => x.IsPublic && x.BaseType.Name == NetworkObjectName).ToArray();
    }

    public TypeDefinition[] NetworkObjectTypes { get; }
}