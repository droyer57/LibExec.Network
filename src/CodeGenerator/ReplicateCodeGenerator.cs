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
    private readonly Dictionary<FieldDefinition, ushort> _fields = new();
    private readonly Dictionary<PropertyDefinition, ushort> _properties = new();
    private ushort _nextId;

    private MethodReference _updateFieldMethodRef = null!;
    private MethodReference _updatePropertyMethodRef = null!;

    protected override void Process()
    {
        var fields = Resource.NetworkObjectTypes.SelectMany(x => x.Fields).Where(x =>
            x.CustomAttributes.Any(a => a.AttributeType.Name == ReplicateAttributeName)).ToArray();

        foreach (var field in fields)
        {
            _fields.Add(field, _nextId++);
        }

        var properties = Resource.NetworkObjectTypes.SelectMany(x => x.Properties).Where(x =>
            x.CustomAttributes.Any(a => a.AttributeType.Name == ReplicateAttributeName)).ToArray();
        var propertiesMethods = properties.ToDictionary(x => x.SetMethod, x => x);

        _nextId = 0;
        foreach (var property in properties)
        {
            _properties.Add(property, _nextId++);
        }

        var updateFieldMethod = LibModule.Types.First(x => x.Name == NetworkObjectClassName).Methods
            .First(x => x.Name == UpdateFieldMethodName);
        updateFieldMethod.IsPublic = true;
        _updateFieldMethodRef = Module.ImportReference(updateFieldMethod);

        var updatePropertyMethod = LibModule.Types.First(x => x.Name == NetworkObjectClassName).Methods
            .First(x => x.Name == UpdatePropertyMethodName);
        updatePropertyMethod.IsPublic = true;
        _updatePropertyMethodRef = Module.ImportReference(updatePropertyMethod);

        var methods = Module.Types.Where(x => x.IsPublic).SelectMany(x => x.Methods).Where(x => x.HasBody);
        foreach (var method in methods)
        {
            if (method.IsConstructor) continue;

            for (var i = 0; i < method.Body.Instructions.Count; i++)
            {
                var instruction = method.Body.Instructions[i];
                if (instruction.Operand is FieldDefinition field && fields.Contains(field) &&
                    instruction.OpCode == OpCodes.Stfld)
                {
                    Execute(field, i, method.Body.GetILProcessor());
                    break;
                }

                if (instruction.Operand is MethodDefinition met && propertiesMethods.TryGetValue(met, out var prop))
                {
                    Execute(prop, i, method.Body.GetILProcessor());
                    break;
                }
            }
        }
    }

    private void Execute(FieldDefinition field, int index, ExtendedIlProcessor ilProcessor)
    {
        // UpdateField(object newValue, ushort fieldId);

        ilProcessor.Index = index - 1;

        ilProcessor.EmitIndex(OpCodes.Box, field.FieldType);
        ilProcessor.EmitIndex(OpCodes.Ldc_I4, _fields[field]);
        ilProcessor.EmitIndex(OpCodes.Callvirt, _updateFieldMethodRef);
        ilProcessor.Value.RemoveAt(ilProcessor.Index + 1);
    }

    private void Execute(PropertyDefinition property, int index, ExtendedIlProcessor ilProcessor)
    {
        // UpdateField(object newValue, ushort fieldId);

        ilProcessor.Index = index - 1;

        ilProcessor.EmitIndex(OpCodes.Box, property.PropertyType);
        ilProcessor.EmitIndex(OpCodes.Ldc_I4, _properties[property]);
        ilProcessor.EmitIndex(OpCodes.Callvirt, _updatePropertyMethodRef);
        ilProcessor.Value.RemoveAt(ilProcessor.Index + 1);
    }
}