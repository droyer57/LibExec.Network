using System.Reflection;

namespace LibExec.Network;

internal static class ReflectionExtensions
{
    public static IEnumerable<Type> GetTypesByBaseType<T>(this Assembly assembly)
    {
        return assembly.GetTypes().Where(x => x.BaseType == typeof(T));
    }

    public static IEnumerable<Type> GetTypesByAttribute<T>(this Assembly assembly) where T : Attribute
    {
        return assembly.GetTypes().Where(x => x.GetCustomAttribute<T>() != null);
    }

    public static IEnumerable<MethodInfo> GetMethodsByAttribute<T>(this Type type)
        where T : Attribute
    {
        return type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(x => x.GetCustomAttribute<T>() != null);
    }

    public static MethodInfo GetMethodByName(this Type type, string name)
    {
        return type.GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static) ??
               throw new InvalidOperationException(nameof(GetMethodByName));
    }
}