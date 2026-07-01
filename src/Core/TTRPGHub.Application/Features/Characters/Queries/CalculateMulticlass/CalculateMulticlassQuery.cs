using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.Characters.Queries.CalculateMulticlass;

public sealed record ClassLevelInput(string ClassSlug, int Level);

public sealed record CalculateMulticlassQuery(string SystemSlug, List<ClassLevelInput> Classes)
    : IRequest<Result<MulticlassResultDto>>;

public sealed record ClassLevelResultDto(string ClassTitle, int Level, string HitDice, int AverageHpContribution);

public sealed record MulticlassResultDto(
    int TotalLevel, int ProficiencyBonus, List<ClassLevelResultDto> Classes, List<string> HitDicePool);
