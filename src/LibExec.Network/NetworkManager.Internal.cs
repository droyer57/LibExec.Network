using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using HarmonyLib;
using LiteNetLib;
using LiteNetLib.Utils;

namespace LibExec.Network;

public partial class NetworkManager
{
    internal const string NetworkInitClassName = "InternalNetworkInit";
    internal const string Key = "DDurBXaw8sLsYs9x";

    private readonly Harmony _harmony = new(Key);
    private readonly Dictionary<MethodInfo, Action<object, object[]?>> _methods = new();

    private readonly Dictionary<Type, BiDictionary<MethodInfo>> _methodTypes = new();
    private readonly List<MethodInfo> _serverMethodInfos = new();
    internal readonly Dictionary<uint, NetworkObject> NetworkObjects = new();
    internal readonly Dictionary<Type, Action<object>> PacketCallbacks = new();
    internal BiDictionary<Type> NetworkObjectTypes { get; private set; } = null!;
    internal BiDictionary<Type> PacketTypes { get; private set; } = null!;
    internal Type? PlayerType { get; private set; }

    internal NetPacketProcessor NetPacketProcessor { get; } = new();

    private void InitInternal()
    {
        RegisterPacket<InvokeMethodPacket>(OnInvokeMethod);

        var entryAssembly = Assembly.GetEntryAssembly() ?? throw new Exception("Cannot get entry assembly");
        var executingAssembly = Assembly.GetExecutingAssembly();

        var networkObjectTypes = entryAssembly.GetTypes().Where(x => x.BaseType == typeof(NetworkObject)).ToArray();
        var packetTypes = executingAssembly.GetTypes().Where(x => x.GetCustomAttribute<PacketAttribute>() != null)
            .ToArray();

        NetworkObjectTypes = new BiDictionary<Type>(networkObjectTypes);
        PacketTypes = new BiDictionary<Type>(packetTypes);

        PlayerType = networkObjectTypes.FirstOrDefault(x => x.GetCustomAttribute<NetworkPlayerAttribute>() != null);

        Activator.CreateInstance(typeof(InternalNetworkInit), true);
        var networkInitClassType = entryAssembly.GetTypes().First(x => x.Name == NetworkInitClassName);
        Activator.CreateInstance(networkInitClassType, true);

        LoadMethods(networkObjectTypes);
        PatchServerMethods();
    }

    private void LoadMethods(IEnumerable<Type> networkObjectTypes)
    {
        foreach (var type in networkObjectTypes)
        {
            var methods = type.GetMethods().Where(x => x.GetCustomAttribute<ServerAttribute>() != null).ToArray();
            _methodTypes[type] = new BiDictionary<MethodInfo>(methods);

            foreach (var method in methods)
            {
                if (method.GetCustomAttribute<ServerAttribute>() != null)
                {
                    _serverMethodInfos.Add(method);
                    _methods.Add(method, CreateMethod(method));
                }
            }
        }
    }

    private void PatchServerMethods()
    {
        var serverPatch = GetType().GetMethods(BindingFlags.NonPublic | BindingFlags.Static)
            .First(x => x.Name == nameof(ServerPatch));
        foreach (var method in _serverMethodInfos)
        {
            _harmony.Patch(method, new HarmonyMethod(serverPatch));
        }
    }

    internal NetworkObject CreateNetworkObject(Type type)
    {
        if (!_networkObjectsCache.TryGetValue(type, out var creator))
        {
            throw new InvalidOperationException($"{nameof(CreateNetworkObject)}");
        }

        return creator();
    }

    internal object CreatePacket(Type type)
    {
        if (!_packetsCache.TryGetValue(type, out var creator))
        {
            throw new InvalidOperationException($"{nameof(CreatePacket)}");
        }

        return creator();
    }

    internal void RegisterPacket<T>(Action<T> callback) where T : class, new()
    {
        NetPacketProcessor.SubscribeReusable(callback);
    }

    internal void EnsureMethodIsCalledByServer()
    {
        if (!IsServer)
        {
            throw new Exception("This method can only be called by the server.");
        }
    }

    internal void AddNetworkObject(NetworkObject networkObject)
    {
        NetworkObjects.Add(networkObject.Id, networkObject);
        SpawnNetworkObjectEvent?.Invoke(networkObject);
    }

    internal void RemoveNetworkObject(NetworkObject networkObject)
    {
        NetworkObjects.Remove(networkObject.Id);
        DestroyNetworkObjectEvent?.Invoke(networkObject);
    }

    private static Action<object, object[]?> CreateMethod(MethodInfo methodInfo)
    {
        var instance = Expression.Parameter(typeof(object), "instance");
        var parameters = Expression.Parameter(typeof(object[]), "parameters");

        var instanceCast = Expression.Convert(instance, methodInfo.DeclaringType!);

        var parametersCasts = new List<Expression>();
        var parameterInfos = methodInfo.GetParameters();
        for (var i = 0; i < parameterInfos.Length; i++)
        {
            var data = Expression.ArrayIndex(parameters, Expression.Constant(i));
            parametersCasts.Add(Expression.Convert(data, parameterInfos[i].ParameterType));
        }

        var call = Expression.Call(instanceCast, methodInfo, parametersCasts);
        return Expression.Lambda<Action<object, object[]?>>(call, instance, parameters).Compile();
    }

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    private static bool ServerPatch(NetworkObject __instance, MethodInfo __originalMethod, object[] __args)
    {
        if (Instance.IsClientOnly)
        {
            var methodId = Instance._methodTypes[__instance.GetType()].Get(__originalMethod);
            var packet = new InvokeMethodPacket { NetworkObjectId = __instance.Id, MethodId = methodId };

            var writer = new NetDataWriter();
            Instance.NetPacketProcessor.Write(writer, packet);
            Instance.ClientManager.Manager.FirstPeer.Send(writer, DeliveryMethod.ReliableOrdered);
        }

        return Instance.IsServer;
    }

    private void OnInvokeMethod(InvokeMethodPacket packet)
    {
        var instance = NetworkObjects[packet.NetworkObjectId];
        var methodInfo = _methodTypes[instance.GetType()].Get(packet.MethodId);
        var method = _methods[methodInfo];

        method.Invoke(instance, null);
    }
}