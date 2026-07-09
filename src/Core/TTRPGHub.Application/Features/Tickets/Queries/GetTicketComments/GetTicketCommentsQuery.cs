using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.Tickets.Queries.GetTicketComments;

public sealed record GetTicketCommentsQuery(Guid TicketId) : IRequest<Result<List<TicketCommentDto>>>;

public sealed record TicketCommentDto(Guid Id, Guid TicketId, Guid AuthorId, string AuthorUsername, string Body, DateTime CreatedAt);
