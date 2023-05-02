using Sandbox.Components.Views;

namespace Sandbox.Services;

public sealed class GameManager
{
    public Type View { get; private set; } = typeof(Menu);

    public event Action? ViewChangedEvent;

    public void SetView<T>()
    {
        View = typeof(T);
        ViewChangedEvent?.Invoke();
    }
}