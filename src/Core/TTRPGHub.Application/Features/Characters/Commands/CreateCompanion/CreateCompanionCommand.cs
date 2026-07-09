using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.Characters.Commands.CreateCompanion;

public sealed record CreateCompanionCommand(
    Guid CharacterId,
    string Name,
    string Kind,
    int Level,
    int MaxHitPoints,
    int? ArmorClass,
    string? Speed,
    string? AttacksText,
    string? AbilitiesText,
    string? Notes
) : IRequest<Result<CreateCompanionResponse>>;

public sealed record CreateCompanionResponse(Guid CompanionId);
