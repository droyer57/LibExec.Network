using System;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using static CodeGenerator.Constants;

namespace CodeGenerator.Generators;

internal sealed class InitCodeGenerator : CodeGenerator
{
    private const string RegisterNetworkObjectMethodName = "RegisterNetworkObject";
    private MethodReference _getTypeMethodRef = null!;
    private TypeDefinition _networkObjectType = null!;

    private MethodDefinition _registerNetworkObjectMethod = null!;

    protected override void Process()
    {
        var networkManagerType = LibModule.Types.First(x => x.Name == NetworkManagerClassName);
        _networkObjectType = LibModule.Types.First(x => x.Name == NetworkObjectClassName);
        var ctorMethod = networkManagerType.Methods.First(x => x.Name == ConstructorName);

        _registerNetworkObjectMethod = networkManagerType.Methods.First(m => m.Name == RegisterNetworkObjectMethodName);

        _getTypeMethodRef = Module.ImportReference(typeof(Type).GetMethod("GetTypeFromHandle"));

        ExtendedIlProcessor ilProcessor = ctorMethod.Body.GetILProcessor();
        ilProcessor.Index = ctorMethod.Body.Instructions.Count - 2;

        foreach (var type in Resource.NetworkObjectTypes)
        {
            Execute(type, ilProcessor);
            foreach (var constructor in type.GetConstructors())
            {
                ExecuteSetType(type, constructor.Body.GetILProcessor());
            }
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

    private void ExecuteSetType(TypeDefinition type, ExtendedIlProcessor ilProcessor)
    {
        var setMethod = _networkObjectType.Properties.First(x => x.Name == "Type").SetMethod;
        setMethod.IsPublic = true;

        ilProcessor.EmitFirst(OpCodes.Ldarg_0);
        ilProcessor.EmitFirst(OpCodes.Ldtoken, type);
        ilProcessor.EmitFirst(OpCodes.Call, _getTypeMethodRef);
        ilProcessor.EmitFirst(OpCodes.Call, Module.ImportReference(setMethod));
    }
}