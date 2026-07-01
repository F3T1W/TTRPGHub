using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.Tickets.Queries.GetMyTickets;

public sealed record GetMyTicketsQuery(int Page = 1, int PageSize = 20)
    : IRequest<Result<PagedTicketsResult>>;

public sealed record TicketAttachmentDto(Guid Id, string Url, string FileName, string ContentType);

public sealed record TicketDto(
    Guid Id, string Title, string Description, string? ContactInfo,
    string Status, DateTime CreatedAt, DateTime UpdatedAt,
    List<TicketAttachmentDto> Attachments);

public sealed record PagedTicketsResult(List<TicketDto> Items, int Total, int Page, int PageSize)
{
    public int TotalPages => (int)Math.Ceiling((double)Total / PageSize);
}
