using System;
using System.Collections.Generic;
using Mono.Cecil;

namespace CodeGenerator;

internal abstract class CodeGenerator
{
    protected ModuleDefinition LibModule { get; private set; } = null!;
    protected ModuleDefinition Module { get; private set; } = null!;
    protected Resource Resource { get; private set; } = null!;

    public void Initialize(ModuleDefinition libModule, ModuleDefinition module, Resource resource)
    {
        LibModule = libModule;
        Module = module;
        Resource = resource;
        Process();
    }

    protected abstract void Process();

    protected static void Setup<T>(Func<IEnumerable<T>> getData, Action<T> execute)
    {
        var data = getData();
        foreach (var item in data)
        {
            execute(item);
        }
    }
}