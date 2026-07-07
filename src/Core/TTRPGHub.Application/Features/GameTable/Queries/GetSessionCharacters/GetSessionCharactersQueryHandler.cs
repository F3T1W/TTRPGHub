using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.GameTable.Queries.GetSessionCharacters;

// Только для GM: список персонажей всех участников сессии, чтобы можно было привязать
// к жетону на столе и получить актуальные HP/AC без ручного ввода.
internal sealed class GetSessionCharactersQueryHandler(
    IGameSessionRepository sessionRepository,
    ICharacterRepository characterRepository,
    IUserRepository userRepository,
    ICurrentUser currentUser
) : IRequestHandler<GetSessionCharactersQuery, Result<List<SessionCharacterDto>>>
{
    public async Task<Result<List<SessionCharacterDto>>> Handle(GetSessionCharactersQuery query, CancellationToken ct)
    {
        var session = await sessionRepository.GetByIdAsync(new GameSessionId(query.SessionId), ct);
        if (session is null)
            return Error.NotFound(nameof(GameSession));

        if (session.OrganizerId != currentUser.Id)
            return Error.Unauthorized();

        var result = new List<SessionCharacterDto>();
        foreach (var p in session.Participants)
        {
            var user = await userRepository.GetByIdAsync(p.UserId, ct);
            var characters = await characterRepository.GetByOwnerAsync(p.UserId, ct);
            result.AddRange(characters.Select(c => new SessionCharacterDto(
                c.Id.Value, c.Name, c.AvatarUrl, p.UserId.Value, user?.Username ?? "—",
                c.CurrentHitPoints, c.MaxHitPoints, c.ArmorClass)));
        }

        return result;
    }
}
