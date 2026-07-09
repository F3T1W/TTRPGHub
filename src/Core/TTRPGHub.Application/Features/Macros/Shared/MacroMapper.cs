using TTRPGHub.Entities;

namespace TTRPGHub.Features.Macros.Shared;

internal static class MacroMapper
{
    internal static MacroDto ToDto(Macro macro) => new(
        macro.Id, macro.Name, macro.ImageUrl, macro.Type.ToString(), macro.Command,
        macro.HotbarSlot, macro.CreatedAt, macro.UpdatedAt);
}
