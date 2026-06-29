using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.Dnd5e.Spells.Queries.GetSpells;

public sealed record GetDnd5eSpellsQuery(
    string? Search,
    string? School,
    int? Level,
    string? ClassName,
    int Page = 1,
    int PageSize = 30
) : IRequest<Result<PagedResult<SpellSummaryDto>>>;

public sealed record SpellSummaryDto(
    Guid Id,
    string Name,
    int Level,
    string School,
    string CastingTime,
    string Range,
    string Duration,
    bool Concentration,
    bool Ritual,
    string Classes);

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize)
{
    public int TotalPages => (int)Math.Ceiling((double)Total / PageSize);
}
