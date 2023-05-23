using System.Reflection;

namespace LibExec.Network;

internal sealed class FastMethodInfo
{
    private readonly Action<NetworkObject, object[]?> _invoker;

    public FastMethodInfo(MethodInfo methodInfo, ushort id)
    {
        Id = id;
        _invoker = methodInfo.CreateDelegate();
        Params = methodInfo.GetParameters().Select(x => x.ParameterType).ToArray();
    }

    public ushort Id { get; }
    public Type[] Params { get; }

    public void Invoke(NetworkObject instance, object[]? args)
    {
        _invoker.Invoke(instance, args);
    }
}