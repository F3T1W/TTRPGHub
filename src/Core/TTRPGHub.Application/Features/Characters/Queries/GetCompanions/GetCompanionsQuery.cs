using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.Characters.Queries.GetCompanions;

public sealed record GetCompanionsQuery(Guid CharacterId) : IRequest<Result<IReadOnlyList<CompanionDto>>>;

public sealed record CompanionDto(
    Guid Id, string Name, string Kind, int Level,
    int MaxHitPoints, int CurrentHitPoints, int? ArmorClass,
    string? Speed, string? AttacksText, string? AbilitiesText, string? Notes
);
