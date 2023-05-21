using Mono.Cecil;
using Mono.Cecil.Cil;

namespace CodeGenerator;

public sealed class ExtendedIlProcessor
{
    private readonly Instruction _firstInstruction;

    private ExtendedIlProcessor(ILProcessor ilProcessor)
    {
        Value = ilProcessor;
        _firstInstruction = ilProcessor.Body.Instructions[0];
    }

    public ILProcessor Value { get; }
    public MethodBody Body => Value.Body;
    public int Index { get; set; }

    public static implicit operator ExtendedIlProcessor(ILProcessor ilProcessor)
    {
        return new ExtendedIlProcessor(ilProcessor);
    }

    public static implicit operator ILProcessor(ExtendedIlProcessor reference)
    {
        return reference.Value;
    }

    #region EmitFirst

    public void EmitFirst(OpCode opcode)
    {
        var instruction = Value.Create(opcode);
        Value.InsertBefore(_firstInstruction, instruction);
    }

    public void EmitFirst(OpCode opcode, TypeReference type)
    {
        var instruction = Value.Create(opcode, type);
        Value.InsertBefore(_firstInstruction, instruction);
    }

    public void EmitFirst(OpCode opcode, MethodReference method)
    {
        var instruction = Value.Create(opcode, method);
        Value.InsertBefore(_firstInstruction, instruction);
    }

    public void EmitFirst(OpCode opcode, CallSite site)
    {
        var instruction = Value.Create(opcode, site);
        Value.InsertBefore(_firstInstruction, instruction);
    }

    public void EmitFirst(OpCode opcode, FieldReference field)
    {
        var instruction = Value.Create(opcode, field);
        Value.InsertBefore(_firstInstruction, instruction);
    }

    public void EmitFirst(OpCode opcode, string value)
    {
        var instruction = Value.Create(opcode, value);
        Value.InsertBefore(_firstInstruction, instruction);
    }

    public void EmitFirst(OpCode opcode, byte value)
    {
        var instruction = Value.Create(opcode, value);
        Value.InsertBefore(_firstInstruction, instruction);
    }

    public void EmitFirst(OpCode opcode, sbyte value)
    {
        var instruction = Value.Create(opcode, value);
        Value.InsertBefore(_firstInstruction, instruction);
    }

    public void EmitFirst(OpCode opcode, int value)
    {
        var instruction = Value.Create(opcode, value);
        Value.InsertBefore(_firstInstruction, instruction);
    }

    public void EmitFirst(OpCode opcode, long value)
    {
        var instruction = Value.Create(opcode, value);
        Value.InsertBefore(_firstInstruction, instruction);
    }

    public void EmitFirst(OpCode opcode, float value)
    {
        var instruction = Value.Create(opcode, value);
        Value.InsertBefore(_firstInstruction, instruction);
    }

    public void EmitFirst(OpCode opcode, double value)
    {
        var instruction = Value.Create(opcode, value);
        Value.InsertBefore(_firstInstruction, instruction);
    }

    public void EmitFirst(OpCode opcode, Instruction target)
    {
        var instruction = Value.Create(opcode, target);
        Value.InsertBefore(_firstInstruction, instruction);
    }

    public void EmitFirst(OpCode opcode, Instruction[] targets)
    {
        var instruction = Value.Create(opcode, targets);
        Value.InsertBefore(_firstInstruction, instruction);
    }

    public void EmitFirst(OpCode opcode, VariableDefinition variable)
    {
        var instruction = Value.Create(opcode, variable);
        Value.InsertBefore(_firstInstruction, instruction);
    }

    public void EmitFirst(OpCode opcode, ParameterDefinition parameter)
    {
        var instruction = Value.Create(opcode, parameter);
        Value.InsertBefore(_firstInstruction, instruction);
    }

    #endregion

    #region EmitIndex

    public void EmitIndex(OpCode opcode)
    {
        var instruction = Value.Create(opcode);
        Value.InsertAfter(Index++, instruction);
    }

    public void EmitIndex(OpCode opcode, TypeReference type)
    {
        var instruction = Value.Create(opcode, type);
        Value.InsertAfter(Index++, instruction);
    }

    public void EmitIndex(OpCode opcode, MethodReference method)
    {
        var instruction = Value.Create(opcode, method);
        Value.InsertAfter(Index++, instruction);
    }

    public void EmitIndex(OpCode opcode, CallSite site)
    {
        var instruction = Value.Create(opcode, site);
        Value.InsertAfter(Index++, instruction);
    }

    public void EmitIndex(OpCode opcode, FieldReference field)
    {
        var instruction = Value.Create(opcode, field);
        Value.InsertAfter(Index++, instruction);
    }

    public void EmitIndex(OpCode opcode, string value)
    {
        var instruction = Value.Create(opcode, value);
        Value.InsertAfter(Index++, instruction);
    }

    public void EmitIndex(OpCode opcode, byte value)
    {
        var instruction = Value.Create(opcode, value);
        Value.InsertAfter(Index++, instruction);
    }

    public void EmitIndex(OpCode opcode, sbyte value)
    {
        var instruction = Value.Create(opcode, value);
        Value.InsertAfter(Index++, instruction);
    }

    public void EmitIndex(OpCode opcode, int value)
    {
        var instruction = Value.Create(opcode, value);
        Value.InsertAfter(Index++, instruction);
    }

    public void EmitIndex(OpCode opcode, long value)
    {
        var instruction = Value.Create(opcode, value);
        Value.InsertAfter(Index++, instruction);
    }

    public void EmitIndex(OpCode opcode, float value)
    {
        var instruction = Value.Create(opcode, value);
        Value.InsertAfter(Index++, instruction);
    }

    public void EmitIndex(OpCode opcode, double value)
    {
        var instruction = Value.Create(opcode, value);
        Value.InsertAfter(Index++, instruction);
    }

    public void EmitIndex(OpCode opcode, Instruction target)
    {
        var instruction = Value.Create(opcode, target);
        Value.InsertAfter(Index++, instruction);
    }

    public void EmitIndex(OpCode opcode, Instruction[] targets)
    {
        var instruction = Value.Create(opcode, targets);
        Value.InsertAfter(Index++, instruction);
    }

    public void EmitIndex(OpCode opcode, VariableDefinition variable)
    {
        var instruction = Value.Create(opcode, variable);
        Value.InsertAfter(Index++, instruction);
    }

    public void EmitIndex(OpCode opcode, ParameterDefinition parameter)
    {
        var instruction = Value.Create(opcode, parameter);
        Value.InsertAfter(Index++, instruction);
    }

    #endregion
}