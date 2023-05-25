using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using static CodeGenerator.Constants;

namespace CodeGenerator;

internal sealed class ReplicateCodeGenerator : CodeGenerator
{
    private const string UpdateFieldMethodName = "UpdateField";
    private const string UpdatePropertyMethodName = "UpdateProperty";
    private readonly Dictionary<FieldDefinition, ushort> _fieldsId = new();
    private readonly Dictionary<PropertyDefinition, ushort> _propertiesId = new();
    private HashSet<FieldDefinition> _fields = null!;
    private ushort _nextId;
    private Dictionary<MethodDefinition, PropertyDefinition> _propertiesMethods = null!;

    private MethodReference _updateFieldMethodRef = null!;
    private MethodReference _updatePropertyMethodRef = null!;

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
                    Execute(field.FieldType, ilProcessor, _fieldsId[field], _updateFieldMethodRef);
                    break;
                }

                if (instruction.Operand is MethodDefinition item && _propertiesMethods.TryGetValue(item, out var prop))
                {
                    Execute(prop.PropertyType, ilProcessor, _propertiesId[prop], _updatePropertyMethodRef);
                    break;
                }
            }
        }
    }

    private void Init()
    {
        _fields = Resource.NetworkObjectTypes.SelectMany(x => x.Fields).Where(x =>
            x.CustomAttributes.Any(a => a.AttributeType.Name == ReplicateAttributeName)).ToHashSet();

        foreach (var field in _fields)
        {
            _fieldsId.Add(field, _nextId++);
        }

        var properties = Resource.NetworkObjectTypes.SelectMany(x => x.Properties).Where(x =>
            x.CustomAttributes.Any(a => a.AttributeType.Name == ReplicateAttributeName)).ToArray();
        _propertiesMethods = properties.ToDictionary(x => x.SetMethod, x => x);

        _nextId = 0;
        foreach (var property in properties)
        {
            _propertiesId.Add(property, _nextId++);
        }

        var updateFieldMethod = LibModule.Types.First(x => x.Name == NetworkObjectClassName).Methods
            .First(x => x.Name == UpdateFieldMethodName);
        updateFieldMethod.IsPublic = true;
        _updateFieldMethodRef = Module.ImportReference(updateFieldMethod);

        var updatePropertyMethod = LibModule.Types.First(x => x.Name == NetworkObjectClassName).Methods
            .First(x => x.Name == UpdatePropertyMethodName);
        updatePropertyMethod.IsPublic = true;
        _updatePropertyMethodRef = Module.ImportReference(updatePropertyMethod);
    }

    private void Execute(TypeReference type, ExtendedIlProcessor ilProcessor, ushort id, MethodReference methodRef)
    {
        // UpdateField(object newValue, ushort id);

        ilProcessor.EmitIndex(OpCodes.Box, type);
        ilProcessor.EmitIndex(OpCodes.Ldc_I4, id);
        ilProcessor.EmitIndex(OpCodes.Callvirt, methodRef);
        ilProcessor.Value.RemoveAt(ilProcessor.Index + 1);
    }
}