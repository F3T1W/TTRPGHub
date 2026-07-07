using TTRPGHub.Entities;

namespace TTRPGHub.Features.GameTable.Shared;

public sealed record JournalEntryDto(
    Guid Id, string Title, string ContentMarkdown, bool IsPublished,
    Guid? ParentId, Guid? CampaignId, List<Guid>? VisibleToUserIds,
    DateTime CreatedAt, DateTime UpdatedAt);

internal static class JournalEntryMapper
{
    internal static JournalEntryDto ToDto(JournalEntry e) =>
        new(e.Id, e.Title, e.ContentMarkdown, e.IsPublished,
            e.ParentId, e.CampaignId?.Value, TableTokenMapper.ParseVisibleTo(e.VisibleToUserIdsJson),
            e.CreatedAt, e.UpdatedAt);
}
