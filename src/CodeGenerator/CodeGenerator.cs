using System;
using System.Collections.Generic;
using Mono.Cecil;

namespace CodeGenerator;

internal abstract class CodeGenerator
{
    protected ModuleDefinition LibModule { get; private set; } = null!;
    protected ModuleDefinition AppModule { get; private set; } = null!;

    public void Initialize(ModuleDefinition libModule, ModuleDefinition appModule)
    {
        LibModule = libModule;
        AppModule = appModule;
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