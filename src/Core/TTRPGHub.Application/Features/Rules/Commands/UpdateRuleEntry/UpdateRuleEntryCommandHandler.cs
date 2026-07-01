using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.Rules.Commands.UpdateRuleEntry;

internal sealed class UpdateRuleEntryCommandHandler(
    IGameSystemRepository systems,
    IRuleEntryRepository entries,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork
) : IRequestHandler<UpdateRuleEntryCommand, Result>
{
    public async Task<Result> Handle(UpdateRuleEntryCommand request, CancellationToken ct)
    {
        var system = await systems.GetBySlugAsync(request.SystemSlug, ct);
        if (system is null)
            return Error.NotFound("GameSystem");

        if (system.IsOfficial || system.CreatedByUserId != currentUser.Id)
            return Error.Forbidden();

        var entry = await entries.GetBySlugAsync(system.Id, request.Category, request.Slug, ct);
        if (entry is null)
            return Error.NotFound("RuleEntry");

        entry.Update(
            request.Title, request.Summary, request.ContentMarkdown,
            string.IsNullOrWhiteSpace(request.StatsJson) ? "{}" : request.StatsJson,
            request.Tags);

        entries.Update(entry);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }
}
