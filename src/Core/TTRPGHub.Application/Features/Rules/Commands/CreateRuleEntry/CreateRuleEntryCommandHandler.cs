using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Features.Rules.Common;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.Rules.Commands.CreateRuleEntry;

internal sealed class CreateRuleEntryCommandHandler(
    IGameSystemRepository systems,
    IRuleEntryRepository entries,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork
) : IRequestHandler<CreateRuleEntryCommand, Result<CreateRuleEntryResponse>>
{
    public async Task<Result<CreateRuleEntryResponse>> Handle(CreateRuleEntryCommand request, CancellationToken ct)
    {
        var system = await systems.GetBySlugAsync(request.SystemSlug, ct);
        if (system is null)
            return Error.NotFound("GameSystem");

        if (system.IsOfficial || system.CreatedByUserId != currentUser.Id)
            return Error.Forbidden();

        var baseSlug = SlugGenerator.FromTitle(request.Title);
        var slug = baseSlug;
        var suffix = 2;
        while (await entries.GetBySlugAsync(system.Id, request.Category, slug, ct) is not null)
            slug = $"{baseSlug}-{suffix++}";

        var entry = RuleEntry.Create(
            system.Id, request.Category, slug, request.Title,
            request.Summary, request.ContentMarkdown,
            string.IsNullOrWhiteSpace(request.StatsJson) ? "{}" : request.StatsJson,
            request.Tags, isHomebrew: true, source: "Homebrew");

        await entries.AddAsync(entry, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return new CreateRuleEntryResponse(entry.Id.Value, entry.Slug);
    }
}
