using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.Characters.Commands.UpdateCompanion;

public sealed record UpdateCompanionCommand(
    Guid CompanionId,
    string Name,
    string Kind,
    int Level,
    int MaxHitPoints,
    int CurrentHitPoints,
    int? ArmorClass,
    string? Speed,
    string? AttacksText,
    string? AbilitiesText,
    string? Notes
) : IRequest<Result>;
