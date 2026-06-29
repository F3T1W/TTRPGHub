using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.Events.Commands.CreateEvent;

public sealed record CreateEventCommand(
    string Title, string? Description, string System,
    string Format, string? Location, string? OnlineLink,
    DateTime StartsAt, int MaxParticipants)
    : IRequest<Result<Guid>>;
