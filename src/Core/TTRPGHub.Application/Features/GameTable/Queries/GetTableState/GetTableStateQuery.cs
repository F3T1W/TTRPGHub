using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Features.GameTable.Shared;

namespace TTRPGHub.Features.GameTable.Queries.GetTableState;

public sealed record GetTableStateQuery(Guid SessionId) : IRequest<Result<TableStateDto>>;

public sealed record TableParticipantDto(Guid UserId, string Username, string? AvatarUrl, bool IsDungeonMaster);

public sealed record TableStateDto(
    Guid SessionId, string Title, string? ShowcaseImageUrl,
    bool IsOrganizer, bool CanAccess,
    List<TableParticipantDto> Participants,
    List<TableMessageDto> RecentMessages,
    AudioStateDto Audio,
    List<TableTokenDto> Tokens);
