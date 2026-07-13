using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Entities.Ratings;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.Ratings.Commands.DeleteSessionReview;

internal sealed class DeleteSessionReviewCommandHandler(
    ISessionReviewRepository reviews,
    ICurrentUser currentUser,
    IUnitOfWork uow
) : IRequestHandler<DeleteSessionReviewCommand, Result>
{
    public async Task<Result> Handle(DeleteSessionReviewCommand request, CancellationToken ct)
    {
        var review = await reviews.GetByIdAsync(SessionReviewId.From(request.ReviewId), ct);
        if (review is null)
            return Error.NotFound(nameof(SessionReview));

        var isModerator = currentUser.Role is UserRole.Moderator or UserRole.Admin;
        if (review.ReviewerId != currentUser.Id && !isModerator)
            return Error.Forbidden();

        reviews.Remove(review);
        await uow.SaveChangesAsync(ct);
        return Result.Success();
    }
}
