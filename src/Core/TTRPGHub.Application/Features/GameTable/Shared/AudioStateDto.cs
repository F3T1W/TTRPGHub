namespace TTRPGHub.Features.GameTable.Shared;

public sealed record AudioStateDto(
    string? TrackUrl, string? TrackTitle,
    bool IsPlaying, double PositionSeconds, DateTime ServerTimestamp);
