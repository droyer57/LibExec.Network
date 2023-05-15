namespace LibExec.Network;

internal class BiDictionary<T1> : BiDictionary<T1, ushort> where T1 : notnull
{
    public BiDictionary(IEnumerable<T1> data) : base(data)
    {
    }

    public BiDictionary()
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

    public BiDictionary()
    {
        _data = new Dictionary<T2, T1>();
        _reverseData = new Dictionary<T1, T2>();
    }

    public int Count => _data.Count;

    public T1 Get(T2 key)
    {
        return _data[key];
    }

    public T2 Get(T1 type)
    {
        return _reverseData[type];
    }

    public void Add(T2 key, T1 value)
    {
        _data.Add(key, value);
        _reverseData.Add(value, key);
    }
}