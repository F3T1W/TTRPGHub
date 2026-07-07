using TTRPGHub.Entities;

namespace TTRPGHub.Features.GameTable.Shared;

// J.2 — порядок хода не хранится отдельной сущностью: вычисляется на лету из токенов сцены
// с заданной Initiative, сортировкой по убыванию (при равенстве — по Id, для стабильного
// порядка между вызовами). Токены без инициативы (декорации, ещё не вступившие в бой) в бою
// не участвуют и здесь не появляются.
internal static class InitiativeOrder
{
    internal static List<TableToken> Sorted(IEnumerable<TableToken> tokens) =>
        tokens
            .Where(t => t.Initiative is not null)
            .OrderByDescending(t => t.Initiative)
            .ThenBy(t => t.Id)
            .ToList();
}
