using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using static CodeGenerator.Constants;

namespace CodeGenerator;

internal sealed class RpcCodeGenerator : CodeGenerator
{
    private readonly Dictionary<MethodDefinition, ushort> _methods = new();
    private MethodDefinition _clientPatchMethod = null!;
    private MethodDefinition _multicastPatchMethod = null!;
    private ushort _nextMethodId;
    private MethodDefinition _serverPatchMethod = null!;

    protected override void Process()
    {
        var serverMethods = Resource.NetworkObjectMethods
            .Where(x => x.CustomAttributes.Any(a => a.AttributeType.Name == ServerAttributeName)).ToArray();
        var clientMethods = Resource.NetworkObjectMethods
            .Where(x => x.CustomAttributes.Any(a => a.AttributeType.Name == ClientAttributeName)).ToArray();
        var multicastMethods = Resource.NetworkObjectMethods
            .Where(x => x.CustomAttributes.Any(a => a.AttributeType.Name == MulticastAttributeName)).ToArray();

        AddMethods(serverMethods);
        AddMethods(clientMethods);
        AddMethods(multicastMethods);

        _serverPatchMethod = LibModule.Types.SelectMany(x => x.Methods).First(x => x.Name == ServerPatchName);
        _clientPatchMethod = LibModule.Types.SelectMany(x => x.Methods).First(x => x.Name == ClientPatchName);
        _multicastPatchMethod = LibModule.Types.SelectMany(x => x.Methods).First(x => x.Name == MulticastPatchName);

        _serverPatchMethod.IsPublic = true;
        _clientPatchMethod.IsPublic = true;
        _multicastPatchMethod.IsPublic = true;

        ProcessMethods(serverMethods, Module.ImportReference(_serverPatchMethod));
        ProcessMethods(clientMethods, Module.ImportReference(_clientPatchMethod));
        ProcessMethods(multicastMethods, Module.ImportReference(_multicastPatchMethod));
    }

    private void AddMethods(IEnumerable<MethodDefinition> methods)
    {
        foreach (var method in methods)
        {
            _methods.Add(method, _nextMethodId++);
        }
    }

    private void ProcessMethods(IEnumerable<MethodDefinition> methods, MethodReference patchMethodRef)
    {
        foreach (var method in methods)
        {
            Execute(method, patchMethodRef);
        }
    }

    private void Execute(MethodDefinition method, MethodReference patchMethodRef)
    {
        // ServerPatch(NetworkObject instance, ushort methodId, object[] args)

        ExtendedIlProcessor extendedIlProcessor = method.Body.GetILProcessor();

        extendedIlProcessor.EmitFirst(OpCodes.Ldarg_0);
        extendedIlProcessor.EmitFirst(OpCodes.Ldc_I4, _methods[method]);
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

        extendedIlProcessor.EmitFirst(OpCodes.Call, patchMethodRef);
        extendedIlProcessor.EmitFirst(OpCodes.Brfalse, method.Body.Instructions[^1]);
    }
}