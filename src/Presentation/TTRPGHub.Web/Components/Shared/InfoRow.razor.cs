using Microsoft.AspNetCore.Components;

namespace TTRPGHub.Components.Shared;

public partial class InfoRow
{
    [Parameter, EditorRequired] public string Label { get; set; } = "";
    [Parameter] public string? Value { get; set; }
}
