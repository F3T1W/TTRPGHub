using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Features.GameTable.Shared;

namespace TTRPGHub.Features.GameTable.Queries.GetJournalEntries;

public sealed record GetJournalEntriesQuery(Guid SessionId) : IRequest<Result<List<JournalEntryDto>>>;
