using System.Reflection;

namespace LibExec.Network;

internal sealed class FastFieldInfo
{
    private readonly Func<NetworkObject, object> _getter;
    private readonly Action<NetworkObject, object>? _onChange;
    private readonly Action<NetworkObject, object> _setter;

    public FastFieldInfo(FieldInfo fieldInfo, ushort id)
    {
        Id = id;
        _setter = fieldInfo.CreateSetterDelegate();
        _getter = fieldInfo.CreateGetterDelegate();

        Type = fieldInfo.FieldType;
        DeclaringType = fieldInfo.DeclaringType ?? throw new ArgumentNullException(nameof(fieldInfo.DeclaringType));
        Attribute = fieldInfo.GetCustomAttribute<ReplicateAttribute>()!;

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
            InvokeOnChange(instance, oldValue);
        }

        return oldValue;
    }

    public void InvokeOnChange(NetworkObject instance, object oldValue)
    {
        _onChange?.Invoke(instance, oldValue);
    }
}