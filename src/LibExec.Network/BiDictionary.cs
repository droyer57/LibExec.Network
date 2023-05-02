namespace LibExec.Network;

internal sealed class BiDictionary<T> where T : notnull
{
    private readonly Dictionary<byte, T> _data;
    private readonly Dictionary<T, byte> _reverseData;

    public BiDictionary(IEnumerable<T> data)
    {
        byte index = 0;
        _data = data.ToDictionary(_ => index++, x => x);
        _reverseData = _data.ToDictionary(x => x.Value, x => x.Key);
    }

    public T Get(byte key)
    {
        return _data[key];
    }

    public byte Get(T type)
    {
        return _reverseData[type];
    }
}