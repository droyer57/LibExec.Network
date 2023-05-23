using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using static CodeGenerator.Constants;

namespace CodeGenerator;

internal sealed class ReplicateCodeGenerator : CodeGenerator
{
    private const string UpdateFieldMethodName = "UpdateField";
    private readonly Dictionary<FieldDefinition, ushort> _fields = new();
    private ushort _nextFieldId;

    private MethodReference _updateFieldMethodRef = null!;

    protected override void Process()
    {
        var fields = Resource.NetworkObjectTypes.SelectMany(x => x.Fields).Where(x =>
            x.CustomAttributes.Any(a => a.AttributeType.Name == ReplicateAttributeName)).ToArray();

        foreach (var field in fields)
        {
            _fields.Add(field, _nextFieldId++);
        }

        var updateFieldMethod = LibModule.Types.First(x => x.Name == NetworkObjectClassName).Methods
            .First(x => x.Name == UpdateFieldMethodName);
        updateFieldMethod.IsPublic = true;
        _updateFieldMethodRef = Module.ImportReference(updateFieldMethod);

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
                    method.Body.InitLocals = true;
                    method.Body.Variables.Add(new VariableDefinition(Module.TypeSystem.Object));

                    Execute(field, i, method.Body.GetILProcessor());
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
}