using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities.Homebrew;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.Homebrew.Commands.CreateHomebrew;

internal sealed class CreateHomebrewCommandHandler(
    IHomebrewRepository homebrew,
    IUnitOfWork uow,
    ICurrentUser currentUser)
    : IRequestHandler<CreateHomebrewCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateHomebrewCommand request, CancellationToken ct)
    {
        var item = HomebrewItem.Create(
            currentUser.Id,
            request.Title,
            request.Description,
            request.System,
            request.Type,
            request.Content,
            request.Tags);

        homebrew.Add(item);
        await uow.SaveChangesAsync(ct);
        return Result<Guid>.Success(item.Id.Value);
    }
}
