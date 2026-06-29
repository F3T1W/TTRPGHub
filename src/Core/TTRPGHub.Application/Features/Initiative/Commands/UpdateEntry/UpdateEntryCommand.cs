using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Entities;

namespace TTRPGHub.Features.Initiative.Commands.UpdateEntry;

public sealed record UpdateEntryCommand(Guid TrackerId, Guid EntryId, int CurrentHp, EntryStatus Status, string? Notes)
    : IRequest<Result>;
