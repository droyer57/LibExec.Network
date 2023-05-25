using System.Reflection;

namespace LibExec.Network;

internal sealed class Reflection
{
    private static Reflection _instance = null!;

    public Reflection()
    {
        if (_instance != null)
        {
            throw new Exception($"{nameof(Reflection)} can only have one instance");
        }

        _instance = this;

        var entryAssembly = Assembly.GetEntryAssembly() ?? throw new Exception("Cannot get entry assembly");
        var executingAssembly = Assembly.GetExecutingAssembly();

        NetworkObjectTypes = entryAssembly.GetTypesByBaseType<NetworkObject>().ToArray();

        var executingPacketTypes = executingAssembly.GetTypesByAttribute<PacketAttribute>();
        var entryPacketTypes = entryAssembly.GetTypesByAttribute<PacketAttribute>();
        PacketTypes = executingPacketTypes.Concat(entryPacketTypes).ToArray();

        PlayerType = NetworkObjectTypes.FirstOrDefault(x => x.GetCustomAttribute<NetworkPlayerAttribute>() != null);

        ServerMethodInfos = NetworkObjectTypes.SelectMany(x => x.GetMethodsByAttribute<ServerAttribute>()).ToArray();
        MulticastMethodInfos = NetworkObjectTypes.SelectMany(x => x.GetMethodsByAttribute<MulticastAttribute>())
            .ToArray();
        ClientMethodInfos = NetworkObjectTypes.SelectMany(x => x.GetMethodsByAttribute<ClientAttribute>()).ToArray();

        ReplicateFieldInfos =
            NetworkObjectTypes.SelectMany(x => x.GetFieldsByAttribute<ReplicateAttribute>()).ToArray();

        ReplicatePropertyInfos =
            NetworkObjectTypes.SelectMany(x => x.GetPropertiesByAttribute<ReplicateAttribute>()).ToArray();
    }

    public static Type[] NetworkObjectTypes { get; private set; } = null!;
    public static Type[] PacketTypes { get; private set; } = null!;
    public static Type? PlayerType { get; private set; }
    public static MethodInfo[] ServerMethodInfos { get; private set; } = null!;
    public static MethodInfo[] MulticastMethodInfos { get; private set; } = null!;
    public static MethodInfo[] ClientMethodInfos { get; private set; } = null!;
    public static FieldInfo[] ReplicateFieldInfos { get; private set; } = null!;
    public static PropertyInfo[] ReplicatePropertyInfos { get; private set; } = null!;
}