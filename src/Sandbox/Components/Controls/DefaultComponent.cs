using Microsoft.AspNetCore.Components;

namespace Sandbox.Components.Controls;

public abstract class DefaultComponent : ComponentBase
{
    [Parameter] public string? ClassName { get; set; }
    [Parameter] public string? Style { get; set; }
}