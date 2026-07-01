using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.Calendar.Commands.UnsubscribePush;

internal sealed class UnsubscribePushCommandHandler(
    IPushSubscriptionRepository repository,
    IUserCalendarPreferenceRepository preferenceRepository,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser
) : IRequestHandler<UnsubscribePushCommand, Result>
{
    public async Task<Result> Handle(UnsubscribePushCommand command, CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
            return Error.Unauthorized();

        var existing = await repository.GetByEndpointAsync(command.Endpoint, ct);
        if (existing is not null)
            repository.Remove(existing);

        var remaining = await repository.GetByUserIdAsync(currentUser.Id, ct);
        if (remaining.Count <= 1)
        {
            var pref = await preferenceRepository.GetByUserIdAsync(currentUser.Id, ct);
            if (pref is not null)
            {
                pref.SetPushEnabled(false);
                preferenceRepository.Update(pref);
            }
        }

        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }
}
