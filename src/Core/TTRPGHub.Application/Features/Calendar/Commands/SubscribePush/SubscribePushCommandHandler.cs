using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.Calendar.Commands.SubscribePush;

internal sealed class SubscribePushCommandHandler(
    IPushSubscriptionRepository repository,
    IUserCalendarPreferenceRepository preferenceRepository,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser
) : IRequestHandler<SubscribePushCommand, Result>
{
    public async Task<Result> Handle(SubscribePushCommand command, CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
            return Error.Unauthorized();

        var existing = await repository.GetByEndpointAsync(command.Endpoint, ct);
        if (existing is null)
        {
            var subscription = PushSubscription.Create(
                currentUser.Id, command.Endpoint, command.P256dh, command.Auth);
            await repository.AddAsync(subscription, ct);
        }

        var pref = await preferenceRepository.GetByUserIdAsync(currentUser.Id, ct);
        if (pref is null)
        {
            pref = UserCalendarPreference.Create(currentUser.Id, 60);
            pref.SetPushEnabled(true);
            await preferenceRepository.AddAsync(pref, ct);
        }
        else if (!pref.PushEnabled)
        {
            pref.SetPushEnabled(true);
            preferenceRepository.Update(pref);
        }

        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }
}
