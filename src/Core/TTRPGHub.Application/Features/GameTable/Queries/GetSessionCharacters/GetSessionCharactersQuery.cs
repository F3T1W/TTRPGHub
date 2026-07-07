using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.GameTable.Queries.GetSessionCharacters;

public sealed record GetSessionCharactersQuery(Guid SessionId) : IRequest<Result<List<SessionCharacterDto>>>;

public sealed record SessionCharacterDto(
    Guid Id, string Name, string? AvatarUrl, Guid OwnerId, string OwnerUsername,
    int CurrentHitPoints, int MaxHitPoints, int ArmorClass);
