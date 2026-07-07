using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Features.GameTable.Shared;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.GameTable.Queries.GetTableState;

internal sealed class GetTableStateQueryHandler(
    IGameSessionRepository sessionRepository,
    ISceneRepository sceneRepository,
    ITableMessageRepository messageRepository,
    ITableTokenRepository tokenRepository,
    IUserRepository userRepository,
    ICurrentUser currentUser
) : IRequestHandler<GetTableStateQuery, Result<TableStateDto>>
{
    public async Task<Result<TableStateDto>> Handle(GetTableStateQuery query, CancellationToken ct)
    {
        var session = await sessionRepository.GetByIdAsync(new GameSessionId(query.SessionId), ct);
        if (session is null)
            return Error.NotFound(nameof(GameSession));

        if (!session.IsParticipant(currentUser.Id))
            return Error.Unauthorized();

        if (session.Status != SessionStatus.InProgress)
            return Error.Validation("Table.NotInProgress", "Игра ещё не началась.");

        var scenes = await sceneRepository.GetBySessionAsync(session.Id, ct);
        var activeScene = scenes.FirstOrDefault(s => s.Id == session.ActiveSceneId) ?? scenes.FirstOrDefault();
        if (activeScene is null)
            return Error.Validation("Scene.NoActiveScene", "У сессии нет ни одной сцены.");

        var participants = new List<TableParticipantDto>();
        foreach (var p in session.Participants)
        {
            var user = await userRepository.GetByIdAsync(p.UserId, ct);
            participants.Add(new TableParticipantDto(
                p.UserId.Value, user?.Username ?? "—", user?.Profile?.AvatarUrl, p.Role == ParticipantRole.DungeonMaster));
        }

        var messages = await messageRepository.GetRecentAsync(session.Id, 100, ct);
        var messageDtos = messages
            .Where(m => m.IsVisibleTo(currentUser.Id))
            .OrderBy(m => m.CreatedAt)
            .Select(m => new TableMessageDto(
                m.Id, m.SenderId.Value, m.SenderUsername,
                m.RecipientId?.Value, m.RecipientUsername,
                m.Kind, m.Content, m.CreatedAt))
            .ToList();

        var isOrganizer = session.OrganizerId == currentUser.Id;
        var tokens = await tokenRepository.GetBySceneAsync(activeScene.Id, ct);
        var tokenDtos = tokens
            .Where(t => t.IsVisibleTo(currentUser.Id, isOrganizer, TableTokenMapper.ParseVisibleTo(t.VisibleToJson)))
            .Select(t => TableTokenMapper.ToDto(t, t.CanBeMovedBy(currentUser.Id, isOrganizer)))
            .ToList();

        return new TableStateDto(
            session.Id.Value, session.Title, activeScene.ShowcaseImageUrl, activeScene.GridCellSizePx,
            isOrganizer, true,
            participants, messageDtos, AudioStateMapper.ToDto(session), tokenDtos,
            activeScene.FogEnabled, activeScene.VisionRadiusFeet, activeScene.WallsJson,
            activeScene.CombatActive, activeScene.CombatRound, activeScene.CombatTurnTokenId,
            activeScene.LightsJson,
            activeScene.TerrainTagsJson, activeScene.AmbientLighting,
            scenes.Select(s => new SceneSummaryDto(s.Id, s.Name)).ToList(), activeScene.Id);
    }
}
