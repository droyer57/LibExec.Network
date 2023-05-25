using System.Reflection;

namespace LibExec.Network;

internal sealed class FastMemberInfo
{
    private readonly Func<NetworkObject, object> _getter;
    private readonly Action<NetworkObject, object>? _onChange;
    private readonly Action<NetworkObject, object> _setter;

    public FastMemberInfo(MemberInfo memberInfo, ushort id)
    {
        Id = id;
        _setter = memberInfo.CreateSetterDelegate();
        _getter = memberInfo.CreateGetterDelegate();

        Type = memberInfo is FieldInfo field ? field.FieldType : ((PropertyInfo)memberInfo).PropertyType;
        DeclaringType = memberInfo.DeclaringType ??
                        throw new ArgumentNullException(nameof(memberInfo.DeclaringType));
        Attribute = memberInfo.GetCustomAttribute<ReplicateAttribute>()!;

        if (Attribute.OnChange != null)
        {
            var onChangeMethod = DeclaringType.GetMethod(Attribute.OnChange,
                                     BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) ??
                                 throw new ArgumentNullException();
            _onChange = onChangeMethod.CreateOnChangeDelegate();
        }
    }

    public ushort Id { get; }
    public Type Type { get; }
    public Type DeclaringType { get; }
    public ReplicateAttribute Attribute { get; }

    public object GetValue(NetworkObject instance)
    {
        return _getter.Invoke(instance);
    }

    public object SetValue(NetworkObject instance, object value)
    {
        var oldValue = GetValue(instance);
        if (value != oldValue)
        {
            _setter.Invoke(instance, value);
            _onChange?.Invoke(instance, oldValue);
        }

        return oldValue;
    }
}