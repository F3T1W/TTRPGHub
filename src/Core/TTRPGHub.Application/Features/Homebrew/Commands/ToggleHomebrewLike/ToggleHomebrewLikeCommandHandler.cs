using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities.Homebrew;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.Homebrew.Commands.ToggleHomebrewLike;

internal sealed class ToggleHomebrewLikeCommandHandler(
    IHomebrewRepository homebrew,
    IUnitOfWork uow,
    ICurrentUser currentUser)
    : IRequestHandler<ToggleHomebrewLikeCommand, Result<ToggleHomebrewLikeResponse>>
{
    public async Task<Result<ToggleHomebrewLikeResponse>> Handle(ToggleHomebrewLikeCommand request, CancellationToken ct)
    {
        var itemId = HomebrewItemId.From(request.ItemId);
        var userId = currentUser.Id;

        var item = await homebrew.GetByIdAsync(itemId, ct);
        if (item is null)
            return Error.NotFound(nameof(item));

        var hasLike = await homebrew.HasLikeAsync(itemId, userId, ct);
        bool liked;

        if (hasLike)
        {
            var like = item.Likes.First(l => l.UserId == userId);
            homebrew.RemoveLike(like);
            liked = false;
        }
        else
        {
            homebrew.AddLike(HomebrewLike.Create(itemId, userId));
            liked = true;
        }

        await uow.SaveChangesAsync(ct);
        return Result<ToggleHomebrewLikeResponse>.Success(
            new ToggleHomebrewLikeResponse(liked, item.Likes.Count + (liked ? 1 : -1)));
    }
}
