using LibExec.Network;
using Microsoft.AspNetCore.Components;

namespace Sandbox.Components;

public abstract class NetworkComponentBase : ComponentBase, IDisposable
{
    [Inject] private NetworkManager NetworkManager { get; init; } = null!;

    public void Dispose()
    {
        NetworkManager.ServerManager.ConnectionStateChangedEvent -= OnServerConnectionStateChanged;
        NetworkManager.ClientManager.ConnectionStateChangedEvent -= OnClientConnectionStateChanged;
        NetworkManager.NetworkObjectEvent -= OnNetworkObjectEvent;
        NetworkManager.NetworkEvent -= OnNetworkEvent;
    }

    protected override void OnAfterRender(bool firstRender)
    {
        if (!firstRender) return;

        NetworkManager.ServerManager.ConnectionStateChangedEvent += OnServerConnectionStateChanged;
        NetworkManager.ClientManager.ConnectionStateChangedEvent += OnClientConnectionStateChanged;
        NetworkManager.NetworkObjectEvent += OnNetworkObjectEvent;
        NetworkManager.NetworkEvent += OnNetworkEvent;
    }

    private void OnServerConnectionStateChanged(ConnectionState state)
    {
        InvokeAsync(StateHasChanged);
    }

    private void OnClientConnectionStateChanged(ConnectionState state)
    {
        InvokeAsync(StateHasChanged);
    }

    private void OnNetworkObjectEvent(NetworkObject networkObject, NetworkObjectEvent state)
    {
        InvokeAsync(StateHasChanged);
    }

    private void OnNetworkEvent(NetworkEvent networkEvent)
    {
        InvokeAsync(StateHasChanged);
    }
}