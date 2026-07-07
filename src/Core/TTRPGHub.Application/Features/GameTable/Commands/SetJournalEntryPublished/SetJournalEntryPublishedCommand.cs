using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.GameTable.Commands.SetJournalEntryPublished;

public sealed record SetJournalEntryPublishedCommand(Guid SessionId, Guid EntryId, bool Published) : IRequest<Result>;
