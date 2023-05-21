using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using static CodeGenerator.Constants;

namespace CodeGenerator;

internal sealed class ReplicateCodeGenerator : CodeGenerator
{
    private const string SendFieldMethodName = "SendField";

    private MethodReference _sendFieldRef = null!;

    protected override void Process()
    {
        var fields = Resource.Types.SelectMany(x => x.Fields).Where(x =>
            x.CustomAttributes.Any(a => a.AttributeType.Name == ReplicateAttributeName)).ToArray();

        var sendField = LibModule.Types.First(x => x.Name == NetworkManagerClassName).Methods
            .First(x => x.Name == SendFieldMethodName);
        _sendFieldRef = Module.ImportReference(sendField);

        foreach (var method in Resource.Methods)
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
        var idField = field.DeclaringType.BaseType.Resolve().Properties.First(x => x.Name == "Id");

        ilProcessor.Index = index - 6;

        ilProcessor.EmitIndex(OpCodes.Ldarg_0);
        ilProcessor.EmitIndex(OpCodes.Ldfld, field);
        ilProcessor.EmitIndex(OpCodes.Box, field.FieldType);
        ilProcessor.EmitIndex(OpCodes.Stloc_0);

        ilProcessor.Index = index + 4;

        ilProcessor.EmitIndex(OpCodes.Ldc_I4_0); // todo: put id
        ilProcessor.EmitIndex(OpCodes.Ldarg_0);
        ilProcessor.EmitIndex(OpCodes.Call, Module.ImportReference(idField.GetMethod));
        ilProcessor.EmitIndex(OpCodes.Ldloc_0);
        ilProcessor.EmitIndex(OpCodes.Ldarg_0);
        ilProcessor.EmitIndex(OpCodes.Ldfld, field);
        ilProcessor.EmitIndex(OpCodes.Box, field.FieldType);
        ilProcessor.EmitIndex(OpCodes.Call, _sendFieldRef);
    }
}