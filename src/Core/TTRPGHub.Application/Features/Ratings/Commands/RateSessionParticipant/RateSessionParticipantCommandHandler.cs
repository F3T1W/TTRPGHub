using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Entities.Ratings;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.Ratings.Commands.RateSessionParticipant;

internal sealed class RateSessionParticipantCommandHandler(
    IGameSessionRepository sessions,
    ISessionReviewRepository reviews,
    IUserRepository userRepo,
    ICurrentUser currentUser,
    IUnitOfWork uow
) : IRequestHandler<RateSessionParticipantCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(RateSessionParticipantCommand request, CancellationToken ct)
    {
        var revieweeId = new UserId(request.RevieweeId);
        if (revieweeId == currentUser.Id)
            return Error.Validation("Нельзя оставить отзыв самому себе.");

        var sessionId = new GameSessionId(request.SessionId);
        var session = await sessions.GetByIdAsync(sessionId, ct);
        if (session is null)
            return Error.NotFound(nameof(GameSession));

        if (session.Status != SessionStatus.Completed)
            return Error.Validation("Отзыв можно оставить только по завершённой сессии.");

        if (!session.IsParticipant(currentUser.Id))
            return Error.Forbidden();

        if (!session.IsParticipant(revieweeId))
            return Error.Validation("Этот пользователь не участвовал в сессии.");

        var ratee = await userRepo.GetByIdAsync(revieweeId, ct);
        if (ratee is null)
            return Error.NotFound(nameof(User));

        var existing = await reviews.GetBySessionReviewerRevieweeAsync(sessionId, currentUser.Id, revieweeId, ct);
        if (existing is not null)
        {
            existing.Update(request.Score, request.Comment);
            await uow.SaveChangesAsync(ct);
            return Result<Guid>.Success(existing.Id.Value);
        }

        var review = SessionReview.Create(sessionId, currentUser.Id, revieweeId, request.Score, request.Comment);
        await reviews.AddAsync(review, ct);
        await uow.SaveChangesAsync(ct);
        return Result<Guid>.Success(review.Id.Value);
    }
}
