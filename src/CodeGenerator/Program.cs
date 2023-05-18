using System;

namespace CodeGenerator;

internal static class Program
{
    public static void Main(string[] args)
    {
        if (args.Length != 1)
        {
            Console.WriteLine("usage: ./CodeGenerator <path_to_the_assembly>");
            Environment.Exit(1);
        }

        var generator = new Generator(args[0]);
        generator.AddCodeGenerator<RpcCodeGenerator>();
        generator.Start();
        generator.Save();
    }
}