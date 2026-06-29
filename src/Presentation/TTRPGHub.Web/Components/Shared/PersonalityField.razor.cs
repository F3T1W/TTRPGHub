using Microsoft.AspNetCore.Components;

namespace TTRPGHub.Components.Shared;

public partial class PersonalityField
{
    [Parameter, EditorRequired] public string Label { get; set; } = "";
    [Parameter] public string? Value { get; set; }
    [Parameter] public string? EditValue { get; set; }
    [Parameter] public bool IsEditing { get; set; }
    [Parameter] public EventCallback<string?> OnChange { get; set; }
}
