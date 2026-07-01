using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.Rules.Commands.DeleteRuleEntry;

internal sealed class DeleteRuleEntryCommandHandler(
    IGameSystemRepository systems,
    IRuleEntryRepository entries,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork
) : IRequestHandler<DeleteRuleEntryCommand, Result>
{
    public async Task<Result> Handle(DeleteRuleEntryCommand request, CancellationToken ct)
    {
        var system = await systems.GetBySlugAsync(request.SystemSlug, ct);
        if (system is null)
            return Error.NotFound("GameSystem");

        if (system.IsOfficial || system.CreatedByUserId != currentUser.Id)
            return Error.Forbidden();

        var entry = await entries.GetBySlugAsync(system.Id, request.Category, request.Slug, ct);
        if (entry is null)
            return Error.NotFound("RuleEntry");

        entries.Remove(entry);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }
}
