using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.Macros.Commands.UpdateMacro;

internal sealed class UpdateMacroCommandHandler(
    IMacroRepository macroRepository,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser
) : IRequestHandler<UpdateMacroCommand, Result>
{
    public async Task<Result> Handle(UpdateMacroCommand command, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(command.Name))
            return Error.Validation("Macro.Name", "Название обязательно.");
        if (!Enum.TryParse<MacroType>(command.Type, out var type))
            return Error.Validation("Macro.Type", "Тип макроса должен быть Chat или Script.");

        var macro = await macroRepository.GetByIdAsync(command.MacroId, ct);
        if (macro is null) return Error.NotFound(nameof(Macro));
        if (macro.OwnerId != currentUser.Id) return Error.Unauthorized();

        macro.Update(command.Name.Trim(), command.ImageUrl, type, command.Command);
        macroRepository.Update(macro);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }
}
