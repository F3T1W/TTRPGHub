using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.Macros.Commands.SetMacroHotbarSlot;

internal sealed class SetMacroHotbarSlotCommandHandler(
    IMacroRepository macroRepository,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser
) : IRequestHandler<SetMacroHotbarSlotCommand, Result>
{
    public async Task<Result> Handle(SetMacroHotbarSlotCommand command, CancellationToken ct)
    {
        // R.1 — несколько страниц хотбара: слот теперь 0-29 (3 страницы по 10, страница = slot/10,
        // позиция внутри страницы = slot%10) вместо 0-9 — сам формат хранения не изменился,
        // просто расширен диапазон, UI (Table.razor) делит его на страницы для отображения.
        if (command.Slot is not (-1) and (< 0 or > 29))
            return Error.Validation("Macro.Slot", "Слот хотбара должен быть от 0 до 29 (или -1, чтобы снять).");

        var macro = await macroRepository.GetByIdAsync(command.MacroId, ct);
        if (macro is null) return Error.NotFound(nameof(Macro));
        if (macro.OwnerId != currentUser.Id) return Error.Unauthorized();

        if (command.Slot >= 0)
        {
            var owned = await macroRepository.GetByOwnerAsync(currentUser.Id, ct);
            var occupying = owned.FirstOrDefault(m => m.HotbarSlot == command.Slot && m.Id != macro.Id);
            if (occupying is not null)
            {
                occupying.SetHotbarSlot(-1);
                macroRepository.Update(occupying);
            }
        }

        macro.SetHotbarSlot(command.Slot);
        macroRepository.Update(macro);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }
}
