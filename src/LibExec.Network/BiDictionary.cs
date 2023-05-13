namespace LibExec.Network;

internal class BiDictionary<T1> : BiDictionary<T1, ushort> where T1 : notnull
{
    public BiDictionary(IEnumerable<T1> data) : base(data)
    {
    }
}

internal class BiDictionary<T1, T2> where T1 : notnull where T2 : struct
{
    private readonly Dictionary<T2, T1> _data;
    private readonly Dictionary<T1, T2> _reverseData;

    public BiDictionary(IEnumerable<T1> data)
    {
        dynamic index = 0;
        _data = data.ToDictionary(_ => (T2)index++, x => x);
        _reverseData = _data.ToDictionary(x => x.Value, x => x.Key);
    }

    public T1 Get(T2 key)
    {
        return _data[key];
    }

    public T2 Get(T1 type)
    {
        return _reverseData[type];
    }
}