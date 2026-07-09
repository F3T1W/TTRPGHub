using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.Tickets.Commands.AddTicketComment;

public sealed record AddTicketCommentCommand(Guid TicketId, string Body) : IRequest<Result<Guid>>;
