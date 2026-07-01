using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.Rules.Queries.GetGameSystems;

internal sealed class GetGameSystemsQueryHandler(
    IGameSystemRepository systemRepository,
    ICurrentUser currentUser
) : IRequestHandler<GetGameSystemsQuery, Result<List<GameSystemDto>>>
{
    public async Task<Result<List<GameSystemDto>>> Handle(GetGameSystemsQuery query, CancellationToken ct)
    {
        var systems = await systemRepository.GetAllAsync(ct);
        return systems
            .Select(s => new GameSystemDto(s.Id.Value, s.Slug, s.Name, s.IsOfficial, s.CreatedByUserId == currentUser.Id))
            .ToList();
    }
}
