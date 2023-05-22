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

        var types = NetworkManager.Methods[MethodId].Params;
        for (var i = 0; i < types.Length; i++)
        {
            if (Args[i] == null)
            {
                throw new ArgumentNullException();
            }

            writer.Put(types[i], Args[i]);
        }
    }

    public void Deserialize(NetDataReader reader)
    {
        MethodId = reader.GetUShort();
        NetworkObjectId = reader.GetUInt();

        var types = NetworkManager.Methods[MethodId].Params;
        Args = new object[types.Length];
        for (var i = 0; i < Args.Length; i++)
        {
            Args[i] = reader.Get(types[i]);
        }
    }
}