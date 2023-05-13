using Microsoft.AspNetCore.Components;

namespace Sandbox.Components.Controls;

public abstract class DefaultComponent : ComponentBase
{
    [Parameter] public string? ClassName { get; init; }
    [Parameter] public string? AdditionalClassName { get; init; }
    [Parameter] public string? Style { get; init; }
}