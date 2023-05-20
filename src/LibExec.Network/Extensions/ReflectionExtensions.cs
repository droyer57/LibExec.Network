using System.Linq.Expressions;
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

    public static Action<NetworkObject, object> CreateSetter(this FieldInfo field)
    {
        var targetExp = Expression.Parameter(typeof(NetworkObject), "target");
        var valueExp = Expression.Parameter(typeof(object), "value");

        var targetCast = Expression.Convert(targetExp, field.DeclaringType!);
        var valueCast = Expression.Convert(valueExp, field.FieldType);

        var fieldExp = Expression.Field(targetCast, field);
        var assignExp = Expression.Assign(fieldExp, valueCast);

        var setter = Expression.Lambda<Action<NetworkObject, object>>(assignExp, targetExp, valueExp).Compile();
        return setter;
    }

    public static Action<NetworkObject, object[]?> CreateMethod(this MethodInfo methodInfo)
    {
        var instance = Expression.Parameter(typeof(NetworkObject), "instance");
        var parameters = Expression.Parameter(typeof(object[]), "parameters");

        var instanceCast = Expression.Convert(instance, methodInfo.DeclaringType!);

        var parametersCasts = new List<Expression>();
        var parameterInfos = methodInfo.GetParameters();
        for (var i = 0; i < parameterInfos.Length; i++)
        {
            var data = Expression.ArrayIndex(parameters, Expression.Constant(i));
            parametersCasts.Add(Expression.Convert(data, parameterInfos[i].ParameterType));
        }

        var call = Expression.Call(instanceCast, methodInfo, parametersCasts);
        return Expression.Lambda<Action<NetworkObject, object[]?>>(call, instance, parameters).Compile();
    }
}