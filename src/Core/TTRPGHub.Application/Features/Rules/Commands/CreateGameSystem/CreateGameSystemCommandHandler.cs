using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Features.Rules.Common;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.Rules.Commands.CreateGameSystem;

internal sealed class CreateGameSystemCommandHandler(
    IGameSystemRepository systems,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork
) : IRequestHandler<CreateGameSystemCommand, Result<CreateGameSystemResponse>>
{
    public async Task<Result<CreateGameSystemResponse>> Handle(CreateGameSystemCommand request, CancellationToken ct)
    {
        var baseSlug = SlugGenerator.FromTitle(request.Name);
        var slug = baseSlug;
        var suffix = 2;
        while (await systems.ExistsAsync(slug, ct))
            slug = $"{baseSlug}-{suffix++}";

        var system = GameSystem.CreateCustom(slug, request.Name, currentUser.Id);
        await systems.AddAsync(system, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return new CreateGameSystemResponse(system.Id.Value, system.Slug, system.Name);
    }
}
