using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Features.GameTable.Shared;

namespace TTRPGHub.Features.GameTable.Queries.GetTableState;

public sealed record GetTableStateQuery(Guid SessionId) : IRequest<Result<TableStateDto>>;

public sealed record TableParticipantDto(Guid UserId, string Username, string? AvatarUrl, bool IsDungeonMaster);

// J.4 — список сцен сессии для переключателя на столе; сама TableStateDto ниже описывает
// текущее состояние только АКТИВНОЙ сцены (карта/сетка/туман/стены/свет/бой/токены) — при
// переключении сцены клиент просто перезапрашивает GetTableState целиком (см. Table.razor.cs).
public sealed record SceneSummaryDto(Guid Id, string Name);

public sealed record TableStateDto(
    Guid SessionId, string Title, string? ShowcaseImageUrl, int GridCellSizePx,
    bool IsOrganizer, bool CanAccess,
    List<TableParticipantDto> Participants,
    List<TableMessageDto> RecentMessages,
    AudioStateDto Audio,
    List<TableTokenDto> Tokens,
    bool FogEnabled, int VisionRadiusFeet, string? WallsJson,
    bool CombatActive, int CombatRound, Guid? CombatTurnTokenId,
    string? LightsJson,
    string? TerrainTagsJson, string AmbientLighting,
    List<SceneSummaryDto> Scenes, Guid ActiveSceneId);
