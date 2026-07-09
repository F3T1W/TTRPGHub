using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.Characters.Commands.CreateChronicle;

public sealed record CreateChronicleCommand(
    Guid CharacterId,
    string ScenarioName,
    DateOnly SessionDate,
    string? GmName,
    string? Faction,
    int GoldEarned,
    int AchievementPoints,
    string? BoonsUsed,
    string? Notes
) : IRequest<Result<CreateChronicleResponse>>;

public sealed record CreateChronicleResponse(Guid ChronicleId);
