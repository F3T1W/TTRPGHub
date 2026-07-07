using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.GameTable.Commands.SetJournalEntryVisibility;

public sealed record SetJournalEntryVisibilityCommand(
    Guid SessionId, Guid EntryId, List<Guid>? VisibleToUserIds) : IRequest<Result>;
