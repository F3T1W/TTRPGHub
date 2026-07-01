using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.Calendar.Commands.UpsertCalendarPreference;

internal sealed class UpsertCalendarPreferenceCommandHandler(
    IUserCalendarPreferenceRepository repository,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser
) : IRequestHandler<UpsertCalendarPreferenceCommand, Result<CalendarPreferenceDto>>
{
    public async Task<Result<CalendarPreferenceDto>> Handle(
        UpsertCalendarPreferenceCommand command, CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
            return Error.Unauthorized();

        var existing = await repository.GetByUserIdAsync(currentUser.Id, ct);

        if (existing is null)
        {
            var pref = UserCalendarPreference.Create(currentUser.Id, command.ReminderMinutes);
            await repository.AddAsync(pref, ct);
            await unitOfWork.SaveChangesAsync(ct);
            return new CalendarPreferenceDto(pref.CalendarToken, pref.ReminderMinutes, pref.PushEnabled);
        }

        existing.UpdateReminder(command.ReminderMinutes);
        if (command.RegenerateToken)
            existing.RegenerateToken();

        repository.Update(existing);
        await unitOfWork.SaveChangesAsync(ct);
        return new CalendarPreferenceDto(existing.CalendarToken, existing.ReminderMinutes, existing.PushEnabled);
    }
}
