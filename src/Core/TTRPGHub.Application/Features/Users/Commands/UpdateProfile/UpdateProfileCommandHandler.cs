using System.Globalization;
using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.Users.Commands.UpdateProfile;

internal sealed class UpdateProfileCommandHandler(
    IUserRepository users,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork
) : IRequestHandler<UpdateProfileCommand, Result>
{
    public async Task<Result> Handle(UpdateProfileCommand request, CancellationToken ct)
    {
        var user = await users.GetByIdAsync(currentUser.Id, ct);
        if (user is null)
            return Error.NotFound(nameof(User));

        user.UpdateProfile(
            string.IsNullOrWhiteSpace(request.DisplayName) ? null : request.DisplayName.Trim(),
            string.IsNullOrWhiteSpace(request.Bio) ? null : request.Bio.Trim(),
            NormalizeCity(request.City));

        users.Update(user);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }

    // Геолокация без структурированного справочника: полноценная привязка к координатам
    // осталась вне охвата (нужен геокодер), но хотя бы устраняем самый частый источник дублей
    // при ILIKE-поиске по городу — "Москва" vs "москва" vs "МОСКВА" — приводя к единому регистру
    // на сервере. Клиент дополнительно подсказывает город из курируемого списка (нормализация
    // написания), но не ограничивает ввод только им — свободные города (малые/незнакомые) всё
    // ещё можно вписать вручную.
    private static string? NormalizeCity(string? city)
    {
        if (string.IsNullOrWhiteSpace(city)) return null;
        var trimmed = string.Join(' ', city.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries));
        return CultureInfo.GetCultureInfo("ru-RU").TextInfo.ToTitleCase(trimmed.ToLowerInvariant());
    }
}
