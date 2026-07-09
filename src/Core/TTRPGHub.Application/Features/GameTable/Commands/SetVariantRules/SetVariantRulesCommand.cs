using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.GameTable.Commands.SetVariantRules;

public sealed record SetVariantRulesCommand(
    Guid SessionId, bool ProficiencyWithoutLevel, bool AutomaticBonusProgression,
    bool FreeArchetype, bool GradualAbilityBoosts, bool StaminaVariant) : IRequest<Result>;
