using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.GameTable.Commands.SetTokenVisibility;

// null = виден всем участникам (по умолчанию); [] = скрыт от всех игроков (виден только GM);
// непустой список — виден только перечисленным игрокам (+ GM, + владельцу токена).
public sealed record SetTokenVisibilityCommand(
    Guid SessionId, Guid TokenId, List<Guid>? VisibleToUserIds
) : IRequest<Result>;
