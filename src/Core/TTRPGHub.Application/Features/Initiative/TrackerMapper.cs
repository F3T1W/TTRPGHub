using TTRPGHub.Entities;
using TTRPGHub.Features.Initiative.Queries.GetTrackerDetail;

namespace TTRPGHub.Features.Initiative;

internal static class TrackerMapper
{
    internal static TrackerDetailDto ToDto(InitiativeTracker t, bool isOwner = true) => new(
        t.Id.Value, t.CampaignId.Value, t.OwnerId.Value, t.Name, t.Round,
        t.ActiveEntryIndex, t.IsActive,
        t.Entries.Select(e => new TrackerEntryDto(
            e.Id, e.Name, e.Initiative, e.MaxHp, e.CurrentHp,
            e.ArmorClass, e.Status, e.IsPlayerCharacter, e.Notes, e.SortOrder)).ToList(),
        isOwner, t.UpdatedAt);
}
