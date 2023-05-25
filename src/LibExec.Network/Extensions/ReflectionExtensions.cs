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

    public static IEnumerable<FieldInfo> GetFieldsByAttribute<T>(this Type type)
        where T : Attribute
    {
        return type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(x => x.GetCustomAttribute<T>() != null);
    }

    public static IEnumerable<PropertyInfo> GetPropertiesByAttribute<T>(this Type type)
        where T : Attribute
    {
        return type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(x => x.GetCustomAttribute<T>() != null);
    }

    public static Action<NetworkObject, object[]?> CreateDelegate(this MethodInfo methodInfo)
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

    public static Action<NetworkObject, object> CreateOnChangeDelegate(this MethodInfo methodInfo)
    {
        var instance = Expression.Parameter(typeof(NetworkObject), "instance");
        var value = Expression.Parameter(typeof(object), "value");

        var instanceCast = Expression.Convert(instance, methodInfo.DeclaringType!);

        var parameterInfos = methodInfo.GetParameters();

        var call = parameterInfos.Length == 1
            ? Expression.Call(instanceCast, methodInfo, Expression.Convert(value, parameterInfos[0].ParameterType))
            : Expression.Call(instanceCast, methodInfo);

        return Expression.Lambda<Action<NetworkObject, object>>(call, instance, value).Compile();
    }

    public static Action<NetworkObject, object> CreateSetterDelegate(this MemberInfo memberInfo)
    {
        var targetExp = Expression.Parameter(typeof(NetworkObject), "target");
        var valueExp = Expression.Parameter(typeof(object), "value");

        var targetCast = Expression.Convert(targetExp, memberInfo.DeclaringType!);

        Expression exp = null!;
        Type memberType = null!;
        switch (memberInfo)
        {
            case PropertyInfo property:
                memberType = property.PropertyType;
                exp = Expression.Property(targetCast, property);
                break;
            case FieldInfo field:
                memberType = field.FieldType;
                exp = Expression.Field(targetCast, field);
                break;
        }

        var valueCast = Expression.Convert(valueExp, memberType);
        var assignExp = Expression.Assign(exp, valueCast);

        var setter = Expression.Lambda<Action<NetworkObject, object>>(assignExp, targetExp, valueExp).Compile();
        return setter;
    }

    public static Func<NetworkObject, object> CreateGetterDelegate(this MemberInfo memberInfo)
    {
        var targetExp = Expression.Parameter(typeof(NetworkObject), "target");
        var targetCast = Expression.Convert(targetExp, memberInfo.DeclaringType!);

        Expression exp = memberInfo switch
        {
            PropertyInfo property => Expression.Property(targetCast, property),
            FieldInfo field => Expression.Field(targetCast, field),
            _ => null!
        };

        var castExp = Expression.Convert(exp, typeof(object));
        var getter = Expression.Lambda<Func<NetworkObject, object>>(castExp, targetExp).Compile();
        return getter;
    }
}