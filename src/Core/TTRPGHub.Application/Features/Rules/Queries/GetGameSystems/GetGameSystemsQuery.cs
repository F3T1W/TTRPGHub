using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.Rules.Queries.GetGameSystems;

public sealed record GetGameSystemsQuery : IRequest<Result<List<GameSystemDto>>>;

public sealed record GameSystemDto(Guid Id, string Slug, string Name, bool IsOfficial, bool IsMine);
