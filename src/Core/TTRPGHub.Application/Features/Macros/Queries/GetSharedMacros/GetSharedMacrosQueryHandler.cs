using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Features.Macros.Shared;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.Macros.Queries.GetSharedMacros;

internal sealed class GetSharedMacrosQueryHandler(
    IGameSessionRepository sessionRepository,
    IMacroRepository macroRepository,
    ICurrentUser currentUser
) : IRequestHandler<GetSharedMacrosQuery, Result<List<MacroDto>>>
{
    public async Task<Result<List<MacroDto>>> Handle(GetSharedMacrosQuery query, CancellationToken ct)
    {
        var session = await sessionRepository.GetByIdAsync(new GameSessionId(query.SessionId), ct);
        if (session is null) return Error.NotFound(nameof(GameSession));
        if (session.OrganizerId != currentUser.Id && !session.IsParticipant(currentUser.Id))
            return Error.Unauthorized();

        var macros = await macroRepository.GetByIdsAsync(session.SharedMacroIds, ct);
        return macros.Select(MacroMapper.ToDto).ToList();
    }
}
