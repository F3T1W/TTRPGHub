using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.GameTable.Commands.UpdateJournalEntry;

public sealed record UpdateJournalEntryCommand(
    Guid SessionId, Guid EntryId, string Title, string ContentMarkdown,
    Guid? ParentId = null, Guid? CampaignId = null) : IRequest<Result>;
