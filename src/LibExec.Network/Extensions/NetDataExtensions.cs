using System.Net;
using LiteNetLib.Utils;

namespace LibExec.Network;

public static class NetDataExtensions
{
    private static readonly Dictionary<Type, Action<NetDataWriter, object>> NetWriterActions = new()
    {
        { typeof(byte), (writer, value) => writer.Put((byte)value) },
        { typeof(sbyte), (writer, value) => writer.Put((sbyte)value) },
        { typeof(short), (writer, value) => writer.Put((short)value) },
        { typeof(ushort), (writer, value) => writer.Put((ushort)value) },
        { typeof(int), (writer, value) => writer.Put((int)value) },
        { typeof(uint), (writer, value) => writer.Put((uint)value) },
        { typeof(long), (writer, value) => writer.Put((long)value) },
        { typeof(ulong), (writer, value) => writer.Put((ulong)value) },
        { typeof(float), (writer, value) => writer.Put((float)value) },
        { typeof(double), (writer, value) => writer.Put((double)value) },
        { typeof(bool), (writer, value) => writer.Put((bool)value) },
        { typeof(string), (writer, value) => writer.Put((string)value) },
        { typeof(char), (writer, value) => writer.Put((char)value) },
        { typeof(IPEndPoint), (writer, value) => writer.Put((IPEndPoint)value) }
    };

    private static readonly Dictionary<Type, Func<NetDataReader, object>> NetReaderActions = new()
    {
        { typeof(byte), reader => reader.GetByte() },
        { typeof(sbyte), reader => reader.GetSByte() },
        { typeof(short), reader => reader.GetShort() },
        { typeof(ushort), reader => reader.GetUShort() },
        { typeof(int), reader => reader.GetInt() },
        { typeof(uint), reader => reader.GetUInt() },
        { typeof(long), reader => reader.GetLong() },
        { typeof(ulong), reader => reader.GetULong() },
        { typeof(float), reader => reader.GetFloat() },
        { typeof(double), reader => reader.GetDouble() },
        { typeof(bool), reader => reader.GetBool() },
        { typeof(string), reader => reader.GetString() },
        { typeof(char), reader => reader.GetChar() },
        { typeof(IPEndPoint), reader => reader.GetNetEndPoint() }
    };

    public static void Put(this NetDataWriter writer, Type type, object value)
    {
        NetWriterActions[type].Invoke(writer, value);
    }

    public static object Get(this NetDataReader reader, Type type)
    {
        return NetReaderActions[type].Invoke(reader);
    }
}