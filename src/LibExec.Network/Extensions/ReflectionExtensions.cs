using System.Reflection;

namespace LibExec.Network;

internal static class ReflectionExtensions
{
    public static IEnumerable<Type> GetTypesWithBaseType<T>(this Assembly assembly)
    {
        return assembly.GetTypes().Where(x => x.BaseType == typeof(T));
    }

    public static IEnumerable<Type> GetTypesWithAttribute<T>(this Assembly assembly) where T : Attribute
    {
        return assembly.GetTypes().Where(x => x.GetCustomAttribute<T>() != null);
    }

    public static IEnumerable<MethodInfo> GetMethodsWithAttribute<T>(this Type type)
        where T : Attribute
    {
        return type.GetMethods().Where(x => x.GetCustomAttribute<T>() != null);
    }

    public static MethodInfo GetMethodWithName(this Type type, string name)
    {
        return type.GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static) ??
               throw new InvalidOperationException(nameof(GetMethodWithName));
    }
}