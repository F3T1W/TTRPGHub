using MediatR;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Common;
using TTRPGHub.Entities.Ratings;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.Ratings.Commands.DeleteRating;

internal sealed class DeleteRatingCommandHandler(
    IRatingRepository ratingRepo,
    ICurrentUser currentUser,
    IUnitOfWork uow)
    : IRequestHandler<DeleteRatingCommand, Result>
{
    public async Task<Result> Handle(DeleteRatingCommand request, CancellationToken ct)
    {
        var rating = await ratingRepo.GetByIdAsync(UserRatingId.From(request.RatingId), ct);
        if (rating is null)
            return Error.NotFound(nameof(UserRating));

        if (rating.RaterId != currentUser.Id)
            return Error.Forbidden();

        ratingRepo.Remove(rating);
        await uow.SaveChangesAsync(ct);
        return Result.Success();
    }
}
