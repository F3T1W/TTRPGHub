using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Features.Macros.Shared;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.Macros.Commands.CreateMacro;

internal sealed class CreateMacroCommandHandler(
    IMacroRepository macroRepository,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser
) : IRequestHandler<CreateMacroCommand, Result<MacroDto>>
{
    public async Task<Result<MacroDto>> Handle(CreateMacroCommand command, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(command.Name))
            return Error.Validation("Macro.Name", "Название обязательно.");
        if (!Enum.TryParse<MacroType>(command.Type, out var type))
            return Error.Validation("Macro.Type", "Тип макроса должен быть Chat или Script.");

        var macro = Macro.Create(currentUser.Id, command.Name.Trim(), command.ImageUrl, type, command.Command);
        await macroRepository.AddAsync(macro, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return MacroMapper.ToDto(macro);
    }
}
