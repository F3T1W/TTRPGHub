using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.Characters.Queries.GetChronicles;

public sealed record GetChroniclesQuery(Guid CharacterId) : IRequest<Result<IReadOnlyList<ChronicleDto>>>;

public sealed record ChronicleDto(
    Guid Id,
    string ScenarioName,
    DateOnly SessionDate,
    string? GmName,
    string? Faction,
    int GoldEarned,
    int AchievementPoints,
    string? BoonsUsed,
    string? Notes
);
