using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Entities;

namespace TTRPGHub.Features.Initiative.Queries.GetTrackerDetail;

public sealed record GetTrackerDetailQuery(Guid TrackerId) : IRequest<Result<TrackerDetailDto>>;

public sealed record TrackerDetailDto(
    Guid Id, Guid CampaignId, Guid OwnerId,
    string Name, int Round, int ActiveEntryIndex, bool IsActive,
    IReadOnlyList<TrackerEntryDto> Entries,
    bool IsOwner, DateTime UpdatedAt);

public sealed record TrackerEntryDto(
    Guid Id, string Name, int Initiative, int MaxHp, int CurrentHp,
    int ArmorClass, EntryStatus Status, bool IsPlayerCharacter, string? Notes, int SortOrder);
