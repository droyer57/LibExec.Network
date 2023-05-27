using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using static CodeGenerator.Constants;

namespace CodeGenerator.Generators;

internal sealed class InitCodeGenerator : CodeGenerator
{
    private TypeDefinition _networkObjectType = null!;
    private MethodDefinition _playerClassIdSetMethod = null!;
    private MethodDefinition _registerNetworkObjectMethod = null!;
    private Dictionary<TypeDefinition, ushort> _typesId = null!;

    protected override void Process()
    {
        var networkManagerType = LibModule.Types.First(x => x.Name == NetworkManagerName);
        _networkObjectType = LibModule.Types.First(x => x.Name == NetworkObjectName);
        var ctorMethod = networkManagerType.Methods.First(x => x.Name == ConstructorName);

        _registerNetworkObjectMethod = networkManagerType.Methods.First(m => m.Name == RegisterNetworkObjectName);

        _playerClassIdSetMethod = networkManagerType.Properties.First(x => x.Name == PlayerClassIdName).SetMethod;
        _playerClassIdSetMethod.IsPublic = true;

        ushort nextId = 1;
        _typesId = Resource.NetworkObjectTypes.ToDictionary(x => x, _ => nextId++);

        ExtendedIlProcessor ilProcessor = ctorMethod.Body.GetILProcessor();
        ilProcessor.Index = ctorMethod.Body.Instructions.Count - 2;

        foreach (var type in Resource.NetworkObjectTypes)
        {
            Execute(type, ilProcessor);
            foreach (var constructor in type.GetConstructors())
            {
                Execute(type, constructor);
            }
        }
    }

    private void Execute(TypeDefinition type, ExtendedIlProcessor ilProcessor)
    {
        // this.RegisterNetworkObject<T>(ushort classId)

        var method = _registerNetworkObjectMethod.MakeGenericMethod(LibModule.ImportReference(type));

        CallMethod(ilProcessor, type, method);

        if (!type.HasDefaultConstructor())
        {
            type.CreateDefaultConstructor();
        }

        if (type.CustomAttributes.FirstOrDefault(x => x.AttributeType.Name == NetworkPlayerAttributeName) != null)
        {
            // this.PlayerClassId = _typesId[type] (NetworkManager)
            CallMethod(ilProcessor, type, _playerClassIdSetMethod);
        }
    }

    private void Execute(TypeDefinition type, MethodDefinition method)
    {
        // this.ClassId = _typesId[type] (NetworkObject)

        var setMethod = _networkObjectType.Properties.First(x => x.Name == ClassIdName).SetMethod;
        setMethod.IsPublic = true;

        ExtendedIlProcessor ilProcessor = method.Body.GetILProcessor();
        ilProcessor.Index = method.Body.Instructions.Count - 2;

        CallMethod(ilProcessor, type, Module.ImportReference(setMethod));
    }

    private void CallMethod(ExtendedIlProcessor ilProcessor, TypeDefinition type, MethodReference method)
    {
        ilProcessor.EmitIndex(OpCodes.Ldarg_0);
        ilProcessor.EmitIndex(OpCodes.Ldc_I4, _typesId[type]);
        ilProcessor.EmitIndex(OpCodes.Call, method);
    }
}