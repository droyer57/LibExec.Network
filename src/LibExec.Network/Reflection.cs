using System.Linq.Expressions;
using System.Reflection;

namespace LibExec.Network;

internal sealed class Reflection
{
    private const string NetworkInitClassName = "InternalNetworkInit";
    private static Reflection _instance = null!;

    public Reflection()
    {
        if (_instance != null)
        {
            throw new Exception($"{nameof(Reflection)} can only have one instance");
        }

        _instance = this;

        var entryAssembly = Assembly.GetEntryAssembly() ?? throw new Exception("Cannot get entry assembly");
        var executingAssembly = Assembly.GetExecutingAssembly();

        NetworkObjectTypes = entryAssembly.GetTypesWithBaseType<NetworkObject>().ToArray();

        var executingPacketTypes = executingAssembly.GetTypesWithAttribute<PacketAttribute>();
        var entryPacketTypes = entryAssembly.GetTypesWithAttribute<PacketAttribute>();
        PacketTypes = executingPacketTypes.Concat(entryPacketTypes).ToArray();

        PlayerType = NetworkObjectTypes.FirstOrDefault(x => x.GetCustomAttribute<NetworkPlayerAttribute>() != null);

        ServerMethodInfos = NetworkObjectTypes.SelectMany(x => x.GetMethodsWithAttribute<ServerAttribute>()).ToArray();

        Activator.CreateInstance(typeof(InternalNetworkInit), true);
        var networkInitClassType = entryAssembly.GetTypes().First(x => x.Name == NetworkInitClassName);
        Activator.CreateInstance(networkInitClassType, true);
    }

    public static Type[] NetworkObjectTypes { get; private set; } = null!;
    public static Type[] PacketTypes { get; private set; } = null!;
    public static Type? PlayerType { get; private set; }
    public static MethodInfo[] ServerMethodInfos { get; private set; } = null!;

    public static Action<object, object[]?> CreateMethod(MethodInfo methodInfo)
    {
        var instance = Expression.Parameter(typeof(object), "instance");
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
        return Expression.Lambda<Action<object, object[]?>>(call, instance, parameters).Compile();
    }
}