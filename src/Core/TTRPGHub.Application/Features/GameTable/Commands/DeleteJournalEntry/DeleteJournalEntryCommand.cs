using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.GameTable.Commands.DeleteJournalEntry;

public sealed record DeleteJournalEntryCommand(Guid SessionId, Guid EntryId) : IRequest<Result>;
