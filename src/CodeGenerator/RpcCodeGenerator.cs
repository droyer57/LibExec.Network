using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace CodeGenerator;

internal sealed class RpcCodeGenerator : CodeGenerator
{
    private const string ServerAttributeName = "ServerAttribute";
    private const string ClientAttributeName = "ClientAttribute";
    private const string MulticastAttributeName = "MulticastAttribute";
    private const string GetCurrentMethodName = "GetCurrentMethod";
    private const string ServerPatchName = "ServerPatch";
    private const string ClientPatchName = "ClientPatch";
    private const string MulticastPatchName = "MulticastPatch";
    private readonly Dictionary<string, MethodReference> _methodReferences = new();
    private MethodDefinition _clientPatchMethod = null!;
    private MethodReference _getCurrentMethodRef = null!;
    private MethodDefinition _multicastPatchMethod = null!;
    private TypeReference _objectRef = null!;

    private MethodDefinition _serverPatchMethod = null!;

    protected override void Process()
    {
        _serverPatchMethod = LibModule.Types.SelectMany(x => x.Methods).First(x => x.Name == ServerPatchName);
        _clientPatchMethod = LibModule.Types.SelectMany(x => x.Methods).First(x => x.Name == ClientPatchName);
        _multicastPatchMethod = LibModule.Types.SelectMany(x => x.Methods).First(x => x.Name == MulticastPatchName);

        _getCurrentMethodRef = AppModule.ImportReference(typeof(MethodBase).GetMethod(GetCurrentMethodName));
        _objectRef = AppModule.ImportReference(typeof(object));

        _methodReferences.Add(ServerAttributeName, AppModule.ImportReference(_serverPatchMethod));
        _methodReferences.Add(ClientAttributeName, AppModule.ImportReference(_clientPatchMethod));
        _methodReferences.Add(MulticastAttributeName, AppModule.ImportReference(_multicastPatchMethod));

        Setup(GetData, Execute);
    }

    private IEnumerable<MethodDefinition> GetData()
    {
        return AppModule.Types.Where(x => x.IsPublic).SelectMany(x => x.Methods).Where(x => x.HasBody);
    }

    private void Execute(MethodDefinition method)
    {
        var attribute = method.CustomAttributes.FirstOrDefault(x =>
            x.AttributeType.Name is ServerAttributeName or ClientAttributeName or MulticastAttributeName);

        if (attribute == null) return;

        ExtendedIlProcessor extendedIlProcessor = method.Body.GetILProcessor();
        var ilProcessor = (ILProcessor)extendedIlProcessor;

        extendedIlProcessor.EmitFirst(OpCodes.Ldarg_0);
        extendedIlProcessor.EmitFirst(OpCodes.Call, _getCurrentMethodRef);
        extendedIlProcessor.EmitFirst(OpCodes.Ldc_I4, method.Parameters.Count);
        extendedIlProcessor.EmitFirst(OpCodes.Newarr, _objectRef);

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