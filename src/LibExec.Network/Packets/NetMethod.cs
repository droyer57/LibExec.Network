using LiteNetLib.Utils;

namespace LibExec.Network;

public struct NetMethod : INetSerializable
{
    private static NetworkManager NetworkManager => NetworkManager.Instance;

    public NetMethod(ushort methodId, uint networkObjectId, object[] args)
    {
        MethodId = methodId;
        NetworkObjectId = networkObjectId;
        Args = args;
    }

    public ushort MethodId { get; private set; }
    public uint NetworkObjectId { get; private set; }
    public object[] Args { get; private set; } = null!;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(MethodId);
        writer.Put(NetworkObjectId);

        foreach (var arg in Args)
        {
            NetworkManager.NetWriterActions[arg.GetType()].Invoke(writer, arg);
        }
    }

    public void Deserialize(NetDataReader reader)
    {
        MethodId = reader.GetUShort();
        NetworkObjectId = reader.GetUInt();

        var methodInfo = NetworkManager.MethodInfos.Get(MethodId);
        var types = NetworkManager.MethodsParams[methodInfo];
        Args = new object[types.Length];
        for (var i = 0; i < Args.Length; i++)
        {
            Args[i] = NetworkManager.NetReaderActions[types[i]].Invoke(reader);
        }
    }
}