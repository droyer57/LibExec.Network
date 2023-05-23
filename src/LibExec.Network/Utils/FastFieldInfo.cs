using System.Reflection;

namespace LibExec.Network;

internal sealed class FastFieldInfo
{
    private readonly Func<NetworkObject, object> _getter;
    private readonly Action<NetworkObject, object> _setter;

    public FastFieldInfo(FieldInfo fieldInfo, ushort id)
    {
        Id = id;
        _setter = fieldInfo.CreateSetter();
        _getter = fieldInfo.CreateGetter();

        Type = fieldInfo.FieldType;
        DeclaringType = fieldInfo.DeclaringType ?? throw new ArgumentNullException(nameof(fieldInfo.DeclaringType));
        Attribute = fieldInfo.GetCustomAttribute<ReplicateAttribute>()!;
    }

    public ushort Id { get; }
    public Type Type { get; }
    public Type DeclaringType { get; }
    public ReplicateAttribute Attribute { get; }

    public object GetValue(NetworkObject instance)
    {
        return _getter.Invoke(instance);
    }

    public void SetValue(NetworkObject instance, object value)
    {
        _setter.Invoke(instance, value);
    }
}