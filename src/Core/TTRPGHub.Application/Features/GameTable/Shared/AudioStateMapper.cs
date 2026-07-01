using TTRPGHub.Entities;

namespace TTRPGHub.Features.GameTable.Shared;

internal static class AudioStateMapper
{
    internal static AudioStateDto ToDto(GameSession session) => new(
        session.CurrentTrackUrl, session.CurrentTrackTitle,
        session.IsAudioPlaying, session.AudioPositionSeconds,
        DateTime.SpecifyKind(session.AudioUpdatedAt, DateTimeKind.Utc));
}
