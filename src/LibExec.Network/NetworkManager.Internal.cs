using System.Reflection;

namespace LibExec.Network;

public partial class NetworkManager
{
    internal const string NetworkInitClassName = "InternalNetworkInit";
    internal const string Key = "DDurBXaw8sLsYs9x";
    internal readonly Dictionary<uint, NetworkObject> NetworkObjects = new();
    internal readonly Dictionary<Type, Action<Packet>> PacketCallbacks = new();
    internal BiDictionary<Type> NetworkObjectTypes { get; private set; } = null!;
    internal BiDictionary<Type> PacketTypes { get; private set; } = null!;
    internal Type? PlayerType { get; private set; }

    private void LoadTypes()
    {
        var entryAssembly = Assembly.GetEntryAssembly() ?? throw new Exception("Cannot get entry assembly");
        var executingAssembly = Assembly.GetExecutingAssembly();

        var networkObjectTypes = entryAssembly.GetTypes().Where(x => x.BaseType == typeof(NetworkObject)).ToArray();
        var packetTypes = executingAssembly.GetTypes().Where(x => x.BaseType == typeof(Packet)).ToArray();

        NetworkObjectTypes = new BiDictionary<Type>(networkObjectTypes);
        PacketTypes = new BiDictionary<Type>(packetTypes);

        PlayerType = networkObjectTypes.FirstOrDefault(x => x.GetCustomAttribute<NetworkPlayerAttribute>() != null);

        Activator.CreateInstance(typeof(InternalNetworkInit), true);
        var networkInitClassType = entryAssembly.GetTypes().First(x => x.Name == NetworkInitClassName);
        Activator.CreateInstance(networkInitClassType, true);
    }

    internal NetworkObject CreateNetworkObject(Type type)
    {
        if (!_networkObjectsCache.TryGetValue(type, out var creator))
        {
            throw new InvalidOperationException($"{nameof(CreateNetworkObject)}");
        }

        return creator();
    }

    internal Packet CreatePacket(Type type)
    {
        if (!_packetsCache.TryGetValue(type, out var creator))
        {
            throw new InvalidOperationException($"{nameof(CreatePacket)}");
        }

        return creator();
    }

    internal void RegisterPacket<T>(Action<T> callback) where T : Packet
    {
        PacketCallbacks.Add(typeof(T), x => callback((T)x));
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
}