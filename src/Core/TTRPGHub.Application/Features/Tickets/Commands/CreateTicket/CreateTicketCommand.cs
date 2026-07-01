using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.Tickets.Commands.CreateTicket;

public sealed record CreateTicketCommand(
    string Title,
    string Description,
    string? ContactInfo,
    List<TicketFileUpload> Files
) : IRequest<Result<Guid>>;

public sealed record TicketFileUpload(Stream Content, string FileName, string ContentType, long Length);
