using System;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using static CodeGenerator.Constants;

namespace CodeGenerator;

internal static class Extensions
{
    public static MethodReference MakeGenericMethod(this MethodReference method, params TypeReference[] arguments)
    {
        if (method.GenericParameters.Count != arguments.Length)
        {
            throw new ArgumentException();
        }

        var instance = new GenericInstanceMethod(method);
        foreach (var argument in arguments)
        {
            instance.GenericArguments.Add(argument);
        }

        return instance;
    }

    public static bool HasDefaultConstructor(this TypeDefinition type)
    {
        return type.Methods.Any(x => x.IsConstructor && x.Parameters.Count == 0);
    }

    public static MethodDefinition CreateDefaultConstructor(this TypeDefinition type)
    {
        const MethodAttributes attributes = MethodAttributes.Public | MethodAttributes.HideBySig |
                                            MethodAttributes.SpecialName | MethodAttributes.RTSpecialName;

        var baseCtorRef = type.Module.ImportReference(typeof(object).GetConstructor(Type.EmptyTypes));
        var ctor = new MethodDefinition(ConstructorName, attributes, type.Module.TypeSystem.Void);
        ctor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
        ctor.Body.Instructions.Add(Instruction.Create(OpCodes.Call, baseCtorRef));
        ctor.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));

        type.Methods.Add(ctor);
        return ctor;
    }
}