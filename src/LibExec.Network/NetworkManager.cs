﻿using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using HarmonyLib;
using LiteNetLib;
using LiteNetLib.Utils;

namespace LibExec.Network;

public sealed class NetworkManager
{
    public const string LocalAddress = "localhost";
    public const int DefaultPort = 1995;

    internal const string Key = "DDurBXaw8sLsYs9x";

    private readonly Harmony _harmony = new(Key);
    private readonly Dictionary<MethodInfo, Action<object, object[]?>> _methods = new();

    private readonly Dictionary<Type, BiDictionary<MethodInfo>> _methodTypes = new();
    private readonly Dictionary<Type, Func<NetworkObject>> _networkObjectsCache = new();
    private readonly List<MethodInfo> _serverMethodInfos = new();
    internal readonly Dictionary<uint, NetworkObject> NetworkObjects = new();
    internal readonly Dictionary<Type, Action<object>> PacketCallbacks = new();

    public NetworkManager()
    {
        if (Instance != null)
        {
            throw new Exception($"{nameof(NetworkManager)} can only have one instance");
        }

        Instance = this;

        var _ = new Reflection();
        PacketProcessor = new PacketProcessor();

        PacketProcessor.RegisterType(() => new NetworkObjectType());

        NetworkObjectTypes = new BiDictionary<Type>(Reflection.NetworkObjectTypes);
        PacketTypes = new BiDictionary<Type>(Reflection.PacketTypes);

        ServerManager = new ServerManager();
        ClientManager = new ClientManager();

        RegisterPacket<InvokeMethodPacket>(OnInvokeMethod);

        LoadMethods();
        PatchServerMethods();
    }

    internal BiDictionary<Type> NetworkObjectTypes { get; private set; }
    internal BiDictionary<Type> PacketTypes { get; private set; }

    internal PacketProcessor PacketProcessor { get; }

    public int Port { get; private set; } = DefaultPort;

    public ServerManager ServerManager { get; }
    public ClientManager ClientManager { get; }

    public static NetworkManager Instance { get; private set; } = null!;

    public bool IsServer => ServerManager.IsStarted;
    public bool IsClient => ClientManager.IsStarted;
    public bool IsClientOnly => !IsServer && IsClient;
    public bool IsServerOnly => IsServer && !IsClient;
    public bool IsHost => IsServer && IsClient;
    public bool IsOffline => !IsServer && !IsClient;

    public event Action<NetworkObject>? SpawnNetworkObjectEvent;
    public event Action<NetworkObject>? DestroyNetworkObjectEvent;

    public void StartServer(int? port = null)
    {
        if (ServerManager.IsRunning) return;

        Port = port ?? DefaultPort;
        ServerManager.Start();
    }

    public void StartClient(string? address = null, int? port = null)
    {
        if (ClientManager.IsRunning) return;

        ClientManager.Address = address ?? LocalAddress;
        Port = port ?? DefaultPort;
        ClientManager.Start();
    }

    public void StartLocalClient()
    {
        StartClient(LocalAddress, Port);
    }

    public void StopClient()
    {
        ClientManager.Stop();
    }

    public void StopServer()
    {
        ServerManager.Stop();
    }

    public IEnumerable<T> Query<T>() where T : NetworkObject
    {
        return NetworkObjects.Values.OfType<T>();
    }

    public void RegisterNetworkObject<T>() where T : NetworkObject, new()
    {
        _networkObjectsCache.Add(typeof(T), () => new T());
    }

    private void LoadMethods()
    {
        foreach (var type in Reflection.NetworkObjectTypes)
        {
            var methods = type.GetMethods().Where(x => x.GetCustomAttribute<ServerAttribute>() != null).ToArray();
            _methodTypes[type] = new BiDictionary<MethodInfo>(methods);

            foreach (var method in methods)
            {
                if (method.GetCustomAttribute<ServerAttribute>() != null)
                {
                    _serverMethodInfos.Add(method);
                    _methods.Add(method, Reflection.CreateMethod(method));
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

    internal void RegisterPacket<T>(Action<T> callback) where T : class, new()
    {
        PacketProcessor.RegisterCallback(callback);
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

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    private static bool ServerPatch(NetworkObject __instance, MethodInfo __originalMethod, object[] __args)
    {
        if (Instance.IsClientOnly)
        {
            var methodId = Instance._methodTypes[__instance.GetType()].Get(__originalMethod);
            var packet = new InvokeMethodPacket { NetworkObjectId = __instance.Id, MethodId = methodId };

            var writer = new NetDataWriter();
            Instance.PacketProcessor.Write(writer, packet);
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