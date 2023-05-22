using LibExec.Network;

namespace Sandbox.Services;

public class NetworkUpdateService : BackgroundService
{
    private readonly NetworkManager _networkManager;

    public NetworkUpdateService(NetworkManager networkManager)
    {
        _networkManager = networkManager;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            _networkManager.Update();
            await Task.Delay(_networkManager.UpdateTime, cancellationToken);
        }
    }
}