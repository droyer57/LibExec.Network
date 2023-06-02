using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using static CodeGenerator.Constants;

namespace CodeGenerator.Generators;

internal sealed class ReplicateCodeGenerator : CodeGenerator
{
    private readonly Dictionary<MemberReference, ushort> _membersId = new();
    private HashSet<FieldDefinition> _fields = null!;
    private ushort _nextId;
    private Dictionary<MethodDefinition, PropertyDefinition> _propertiesMethods = null!;

    private MethodReference _updateMemberMethodRef = null!;

    protected override void Process()
    {
        Init();

        var methods = Module.Types.Where(x => x.IsPublic).SelectMany(x => x.Methods).Where(x => x.HasBody);
        foreach (var method in methods)
        {
            if (method.IsConstructor) continue;

            ExtendedIlProcessor ilProcessor = method.Body.GetILProcessor();

            for (var i = 0; i < method.Body.Instructions.Count; i++)
            {
                ilProcessor.Index = i - 1;
                var instruction = method.Body.Instructions[i];

                if (instruction.Operand is FieldDefinition field && _fields.Contains(field) &&
                    instruction.OpCode == OpCodes.Stfld)
                {
                    Execute(field, field.FieldType, ilProcessor);
                }

                if (instruction.Operand is MethodDefinition item && _propertiesMethods.TryGetValue(item, out var prop))
                {
                    Execute(prop, prop.PropertyType, ilProcessor);
                }
            }
        }
    }

    private void Init()
    {
        _fields = Resource.NetworkObjectTypes.SelectMany(x => x.Fields).Where(x =>
            x.CustomAttributes.Any(a => a.AttributeType.Name == ReplicateAttributeName)).ToHashSet();

        var properties = Resource.NetworkObjectTypes.SelectMany(x => x.Properties).Where(x =>
            x.CustomAttributes.Any(a => a.AttributeType.Name == ReplicateAttributeName)).ToArray();
        _propertiesMethods = properties.ToDictionary(x => x.SetMethod, x => x);

        foreach (var field in _fields)
        {
            _membersId.Add(field, _nextId++);
        }

        foreach (var property in properties)
        {
            _membersId.Add(property, _nextId++);
        }

        var updateMemberMethod = LibModule.Types.First(x => x.Name == NetworkObjectName).Methods
            .First(x => x.Name == UpdateMemberName);
        updateMemberMethod.IsPublic = true;
        _updateMemberMethodRef = Module.ImportReference(updateMemberMethod);
    }

    private void Execute(MemberReference member, TypeReference type, ExtendedIlProcessor ilProcessor)
    {
        // UpdateMember(object newValue, ushort id);

        ilProcessor.EmitIndex(OpCodes.Box, type);
        ilProcessor.EmitIndex(OpCodes.Ldc_I4, _membersId[member]);
        ilProcessor.EmitIndex(OpCodes.Callvirt, _updateMemberMethodRef);
        ilProcessor.Value.RemoveAt(ilProcessor.Index + 1);
    }
}