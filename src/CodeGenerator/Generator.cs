using System.Collections.Generic;
using System.IO;
using Mono.Cecil;

namespace CodeGenerator;

internal sealed class Generator
{
    private const string LibName = "LibExec.Network.dll";

    private readonly List<ModuleDefinition> _modules = new();
    private readonly List<CodeGenerator> _processors = new();
    private readonly Resource _resource;

    public Generator(string fileName)
    {
        var libFileName = $"{Path.GetDirectoryName(fileName)}/{LibName}";

        var resolver = new DefaultAssemblyResolver();
        resolver.AddSearchDirectory(Path.GetDirectoryName(fileName));

        var readerParameters = new ReaderParameters
        {
            AssemblyResolver = resolver
        };

        _modules.Add(ModuleDefinition.ReadModule(libFileName, readerParameters));
        _modules.Add(ModuleDefinition.ReadModule(fileName, readerParameters));

        _resource = new Resource(_modules[0], _modules[1]);
    }

    public void Start()
    {
        foreach (var processor in _processors)
        {
            processor.Initialize(_modules[0], _modules[1], _resource);
        }
    }

    public void Save()
    {
        foreach (var module in _modules)
        {
            var generatedFileName = Path.Combine(Path.GetDirectoryName(module.FileName)!, $"{module.FileName}.gen");

            module.Write(generatedFileName);
            module.Dispose();

            File.Copy(generatedFileName, module.FileName, true);
            File.Delete(generatedFileName);
        }
    }

    public void AddCodeGenerator<T>() where T : CodeGenerator, new()
    {
        _processors.Add(new T());
    }
}