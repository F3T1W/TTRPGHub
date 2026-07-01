using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Entities.Homebrew;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.Homebrew.Commands.DeleteHomebrew;

internal sealed class DeleteHomebrewCommandHandler(
    IHomebrewRepository homebrew,
    IUnitOfWork uow,
    ICurrentUser currentUser)
    : IRequestHandler<DeleteHomebrewCommand, Result>
{
    public async Task<Result> Handle(DeleteHomebrewCommand request, CancellationToken ct)
    {
        var item = await homebrew.GetByIdAsync(HomebrewItemId.From(request.Id), ct);
        if (item is null)
            return Error.NotFound(nameof(item));

        var isModerator = currentUser.Role is UserRole.Moderator or UserRole.Admin;
        if (item.AuthorId != currentUser.Id && !isModerator)
            return Error.Forbidden();

        homebrew.Remove(item);
        await uow.SaveChangesAsync(ct);
        return Result.Success();
    }
}
