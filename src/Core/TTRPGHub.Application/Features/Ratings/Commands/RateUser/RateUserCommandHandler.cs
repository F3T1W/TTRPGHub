using MediatR;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Common;
using TTRPGHub.Entities;
using TTRPGHub.Entities.Ratings;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.Ratings.Commands.RateUser;

internal sealed class RateUserCommandHandler(
    IRatingRepository ratingRepo,
    IUserRepository userRepo,
    ICurrentUser currentUser,
    IUnitOfWork uow)
    : IRequestHandler<RateUserCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(RateUserCommand request, CancellationToken ct)
    {
        var rateeId = new UserId(request.RateeId);

        if (rateeId == currentUser.Id)
            return Error.Validation("Нельзя оставить отзыв самому себе.");

        var ratee = await userRepo.GetByIdAsync(rateeId, ct);
        if (ratee is null)
            return Error.NotFound(nameof(User));

        if (!Enum.TryParse<RatingRole>(request.Role, out var role))
            return Error.Validation("Неверная роль. Используй Player или DungeonMaster.");

        var existing = await ratingRepo.GetByRaterAndRateeAsync(currentUser.Id, rateeId, ct);
        if (existing is not null)
        {
            existing.Update(request.Score, request.Comment, role);
            await uow.SaveChangesAsync(ct);
            return Result<Guid>.Success(existing.Id.Value);
        }

        var rating = UserRating.Create(currentUser.Id, rateeId, request.Score, request.Comment, role);
        await ratingRepo.AddAsync(rating, ct);
        await uow.SaveChangesAsync(ct);
        return Result<Guid>.Success(rating.Id.Value);
    }
}
