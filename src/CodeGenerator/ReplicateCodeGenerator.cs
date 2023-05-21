using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using static CodeGenerator.Constants;

namespace CodeGenerator;

internal sealed class ReplicateCodeGenerator : CodeGenerator
{
    private const string SendFieldMethodName = "SendField";
    private readonly Dictionary<FieldDefinition, ushort> _fields = new();
    private ushort _nextFieldId;

    private MethodReference _sendFieldMethodRef = null!;

    protected override void Process()
    {
        var fields = Resource.NetworkObjectTypes.SelectMany(x => x.Fields).Where(x =>
            x.CustomAttributes.Any(a => a.AttributeType.Name == ReplicateAttributeName)).ToArray();

        foreach (var field in fields)
        {
            _fields.Add(field, _nextFieldId++);
        }

        var sendFieldMethod = LibModule.Types.First(x => x.Name == NetworkManagerClassName).Methods
            .First(x => x.Name == SendFieldMethodName);
        sendFieldMethod.IsPublic = true;
        _sendFieldMethodRef = Module.ImportReference(sendFieldMethod);

        foreach (var method in Resource.NetworkObjectMethods)
        {
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
        // SendField(ushort id, uint networkObjectId, object oldValue, object value)

        var idField = field.DeclaringType.BaseType.Resolve().Properties.First(x => x.Name == "Id");

        ilProcessor.Index = index - 6;

        ilProcessor.EmitIndex(OpCodes.Ldarg_0);
        ilProcessor.EmitIndex(OpCodes.Ldfld, field);
        ilProcessor.EmitIndex(OpCodes.Box, field.FieldType);
        ilProcessor.EmitIndex(OpCodes.Stloc_0);

        ilProcessor.Index = index + 4;

        ilProcessor.EmitIndex(OpCodes.Ldc_I4, _fields[field]);
        ilProcessor.EmitIndex(OpCodes.Ldarg_0);
        ilProcessor.EmitIndex(OpCodes.Call, Module.ImportReference(idField.GetMethod));
        ilProcessor.EmitIndex(OpCodes.Ldloc_0);
        ilProcessor.EmitIndex(OpCodes.Ldarg_0);
        ilProcessor.EmitIndex(OpCodes.Ldfld, field);
        ilProcessor.EmitIndex(OpCodes.Box, field.FieldType);
        ilProcessor.EmitIndex(OpCodes.Call, _sendFieldMethodRef);
    }
}