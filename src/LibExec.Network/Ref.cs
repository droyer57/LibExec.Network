namespace LibExec.Network;

internal interface IRef<out T> where T : NetworkObject
{
    void RemoveInstance();
}

public sealed class Ref<T> : IRef<T> where T : NetworkObject
{
    private Ref(T? item)
    {
        if (item == null) return;

        Instance = item;
        NetworkManager.Instance.Refs.Add(Instance, this);
    }

    // public T? Instance => _item?.IsValid() == true ? _item : null;
    public T? Instance { get; private set; }

    public void RemoveInstance()
    {
        Instance = null;
    }

    public static implicit operator Ref<T>(T? value)
    {
        return new Ref<T>(value);
    }

    public static implicit operator T?(Ref<T> reference)
    {
        return reference.Instance;
    }
}