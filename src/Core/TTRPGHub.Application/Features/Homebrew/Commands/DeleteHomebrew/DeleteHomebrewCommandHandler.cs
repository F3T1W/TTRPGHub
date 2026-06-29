using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
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

        if (item.AuthorId != currentUser.Id)
            return Error.Validation("Author", "Можно удалять только свои материалы");

        homebrew.Remove(item);
        await uow.SaveChangesAsync(ct);
        return Result.Success();
    }
}
