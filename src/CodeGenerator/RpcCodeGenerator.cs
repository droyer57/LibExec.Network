using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;
using static CodeGenerator.Constants;

namespace CodeGenerator;

internal sealed class RpcCodeGenerator : CodeGenerator
{
    private const string GetCurrentMethodName = "GetCurrentMethod";
    private readonly Dictionary<string, MethodReference> _methodReferences = new();
    private MethodDefinition _clientPatchMethod = null!;
    private MethodReference _getCurrentMethodRef = null!;
    private MethodDefinition _multicastPatchMethod = null!;
    private MethodDefinition _serverPatchMethod = null!;

    protected override void Process()
    {
        _serverPatchMethod = LibModule.Types.SelectMany(x => x.Methods).First(x => x.Name == ServerPatchName);
        _clientPatchMethod = LibModule.Types.SelectMany(x => x.Methods).First(x => x.Name == ClientPatchName);
        _multicastPatchMethod = LibModule.Types.SelectMany(x => x.Methods).First(x => x.Name == MulticastPatchName);

        _serverPatchMethod.IsPublic = true;
        _clientPatchMethod.IsPublic = true;
        _multicastPatchMethod.IsPublic = true;

        _getCurrentMethodRef = Module.ImportReference(typeof(MethodBase).GetMethod(GetCurrentMethodName));

        _methodReferences.Add(ServerAttributeName, Module.ImportReference(_serverPatchMethod));
        _methodReferences.Add(ClientAttributeName, Module.ImportReference(_clientPatchMethod));
        _methodReferences.Add(MulticastAttributeName, Module.ImportReference(_multicastPatchMethod));

        Setup(GetData, Execute);
    }

    private IEnumerable<MethodDefinition> GetData()
    {
        return Module.Types.Where(x => x.IsPublic).SelectMany(x => x.Methods).Where(x => x.HasBody);
    }

    private void Execute(MethodDefinition method)
    {
        // ServerPatch(this, MethodBase.GetCurrentMethod(), new object[] { ...args });
        // todo: find a better way than MethodBase.GetCurrentMethod()

        var attribute = method.CustomAttributes.FirstOrDefault(x =>
            x.AttributeType.Name is ServerAttributeName or ClientAttributeName or MulticastAttributeName);

        if (attribute == null) return;

        ExtendedIlProcessor extendedIlProcessor = method.Body.GetILProcessor();

        extendedIlProcessor.EmitFirst(OpCodes.Ldarg_0);
        extendedIlProcessor.EmitFirst(OpCodes.Call, _getCurrentMethodRef);
        extendedIlProcessor.EmitFirst(OpCodes.Ldc_I4, method.Parameters.Count);
        extendedIlProcessor.EmitFirst(OpCodes.Newarr, Module.TypeSystem.Object);

        for (var i = 0; i < method.Parameters.Count; i++)
        {
            extendedIlProcessor.EmitFirst(OpCodes.Dup);
            extendedIlProcessor.EmitFirst(OpCodes.Ldc_I4, i);
            extendedIlProcessor.EmitFirst(OpCodes.Ldarg, i + 1);
            extendedIlProcessor.EmitFirst(OpCodes.Box, method.Parameters[i].ParameterType);
            extendedIlProcessor.EmitFirst(OpCodes.Stelem_Ref);
        }

        extendedIlProcessor.EmitFirst(OpCodes.Call, _methodReferences[attribute.AttributeType.Name]);
        extendedIlProcessor.EmitFirst(OpCodes.Brfalse, method.Body.Instructions[^1]);
    }
}