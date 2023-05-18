using Mono.Cecil;
using Mono.Cecil.Cil;

namespace CodeGenerator;

public sealed class ExtendedIlProcessor
{
    private readonly Instruction _firstInstruction;
    private readonly ILProcessor _ilProcessor;

    private ExtendedIlProcessor(ILProcessor ilProcessor)
    {
        _ilProcessor = ilProcessor;
        _firstInstruction = ilProcessor.Body.Instructions[0];
    }

    public static implicit operator ExtendedIlProcessor(ILProcessor ilProcessor)
    {
        return new ExtendedIlProcessor(ilProcessor);
    }

    public static implicit operator ILProcessor(ExtendedIlProcessor reference)
    {
        return reference._ilProcessor;
    }

    public void EmitFirst(OpCode opcode)
    {
        var instruction = _ilProcessor.Create(opcode);
        _ilProcessor.InsertBefore(_firstInstruction, instruction);
    }

    public void EmitFirst(OpCode opcode, TypeReference type)
    {
        var instruction = _ilProcessor.Create(opcode, type);
        _ilProcessor.InsertBefore(_firstInstruction, instruction);
    }

    public void EmitFirst(OpCode opcode, MethodReference method)
    {
        var instruction = _ilProcessor.Create(opcode, method);
        _ilProcessor.InsertBefore(_firstInstruction, instruction);
    }

    public void EmitFirst(OpCode opcode, CallSite site)
    {
        var instruction = _ilProcessor.Create(opcode, site);
        _ilProcessor.InsertBefore(_firstInstruction, instruction);
    }

    public void EmitFirst(OpCode opcode, FieldReference field)
    {
        var instruction = _ilProcessor.Create(opcode, field);
        _ilProcessor.InsertBefore(_firstInstruction, instruction);
    }

    public void EmitFirst(OpCode opcode, string value)
    {
        var instruction = _ilProcessor.Create(opcode, value);
        _ilProcessor.InsertBefore(_firstInstruction, instruction);
    }

    public void EmitFirst(OpCode opcode, byte value)
    {
        var instruction = _ilProcessor.Create(opcode, value);
        _ilProcessor.InsertBefore(_firstInstruction, instruction);
    }

    public void EmitFirst(OpCode opcode, sbyte value)
    {
        var instruction = _ilProcessor.Create(opcode, value);
        _ilProcessor.InsertBefore(_firstInstruction, instruction);
    }

    public void EmitFirst(OpCode opcode, int value)
    {
        var instruction = _ilProcessor.Create(opcode, value);
        _ilProcessor.InsertBefore(_firstInstruction, instruction);
    }

    public void EmitFirst(OpCode opcode, long value)
    {
        var instruction = _ilProcessor.Create(opcode, value);
        _ilProcessor.InsertBefore(_firstInstruction, instruction);
    }

    public void EmitFirst(OpCode opcode, float value)
    {
        var instruction = _ilProcessor.Create(opcode, value);
        _ilProcessor.InsertBefore(_firstInstruction, instruction);
    }

    public void EmitFirst(OpCode opcode, double value)
    {
        var instruction = _ilProcessor.Create(opcode, value);
        _ilProcessor.InsertBefore(_firstInstruction, instruction);
    }

    public void EmitFirst(OpCode opcode, Instruction target)
    {
        var instruction = _ilProcessor.Create(opcode, target);
        _ilProcessor.InsertBefore(_firstInstruction, instruction);
    }

    public void EmitFirst(OpCode opcode, Instruction[] targets)
    {
        var instruction = _ilProcessor.Create(opcode, targets);
        _ilProcessor.InsertBefore(_firstInstruction, instruction);
    }

    public void EmitFirst(OpCode opcode, VariableDefinition variable)
    {
        var instruction = _ilProcessor.Create(opcode, variable);
        _ilProcessor.InsertBefore(_firstInstruction, instruction);
    }

    public void EmitFirst(OpCode opcode, ParameterDefinition parameter)
    {
        var instruction = _ilProcessor.Create(opcode, parameter);
        _ilProcessor.InsertBefore(_firstInstruction, instruction);
    }
}