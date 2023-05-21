using System.Linq;
using Mono.Cecil;
using static CodeGenerator.Constants;

namespace CodeGenerator;

internal sealed class Resource
{
    public Resource(ModuleDefinition libModule, ModuleDefinition module)
    {
        Types = module.Types.Where(x => x.IsPublic && x.BaseType.Name == NetworkObjectClassName).ToArray();
        Methods = Types.SelectMany(x => x.Methods).Where(x => x.HasBody).ToArray();
    }

    public TypeDefinition[] Types { get; }
    public MethodDefinition[] Methods { get; private set; }
}