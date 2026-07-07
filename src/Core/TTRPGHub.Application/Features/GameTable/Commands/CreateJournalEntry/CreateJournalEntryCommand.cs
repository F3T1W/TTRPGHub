using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Features.GameTable.Shared;

namespace TTRPGHub.Features.GameTable.Commands.CreateJournalEntry;

public sealed record CreateJournalEntryCommand(
    Guid SessionId, string Title, string ContentMarkdown,
    Guid? ParentId = null, Guid? CampaignId = null) : IRequest<Result<JournalEntryDto>>;
