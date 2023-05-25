using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using static CodeGenerator.Constants;

namespace CodeGenerator.Generators;

internal sealed class InitCodeGenerator : CodeGenerator
{
    private const string RegisterNetworkObjectMethodName = "RegisterNetworkObject";

    private MethodDefinition _registerNetworkObjectMethod = null!;

    protected override void Process()
    {
        var networkManager = LibModule.Types.First(x => x.Name == NetworkManagerClassName);
        var ctorMethod = networkManager.Methods.First(x => x.Name == ConstructorName);
        var networkObjects = Module.Types.Where(x => x.BaseType?.Name == NetworkObjectClassName);

        _registerNetworkObjectMethod = networkManager.Methods.First(m => m.Name == RegisterNetworkObjectMethodName);

        ExtendedIlProcessor ilProcessor = ctorMethod.Body.GetILProcessor();
        ilProcessor.Index = ctorMethod.Body.Instructions.Count - 2;

        foreach (var type in networkObjects)
        {
            Execute(type, ilProcessor);
        }
    }

    private void Execute(TypeDefinition type, ExtendedIlProcessor ilProcessor)
    {
        var method = _registerNetworkObjectMethod.MakeGenericMethod(LibModule.ImportReference(type));

        ilProcessor.EmitIndex(OpCodes.Ldarg_0);
        ilProcessor.EmitIndex(OpCodes.Call, method);

        if (!type.HasDefaultConstructor())
        {
            type.CreateDefaultConstructor();
        }
    }
}