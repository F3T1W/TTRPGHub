using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.Macros.Commands.DeleteMacro;

internal sealed class DeleteMacroCommandHandler(
    IMacroRepository macroRepository,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser
) : IRequestHandler<DeleteMacroCommand, Result>
{
    public async Task<Result> Handle(DeleteMacroCommand command, CancellationToken ct)
    {
        var macro = await macroRepository.GetByIdAsync(command.MacroId, ct);
        if (macro is null) return Error.NotFound(nameof(Macro));
        if (macro.OwnerId != currentUser.Id) return Error.Unauthorized();

        macroRepository.Remove(macro);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }
}
