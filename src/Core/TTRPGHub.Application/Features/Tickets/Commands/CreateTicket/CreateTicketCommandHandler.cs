using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.Tickets.Commands.CreateTicket;

internal sealed class CreateTicketCommandHandler(
    ISupportTicketRepository tickets,
    ICurrentUser currentUser,
    IStorageService storage,
    IUnitOfWork unitOfWork
) : IRequestHandler<CreateTicketCommand, Result<Guid>>
{
    private const string Bucket = "tickets";
    private const long MaxBytes = 10 * 1024 * 1024; // 10 МБ

    public async Task<Result<Guid>> Handle(CreateTicketCommand request, CancellationToken ct)
    {
        if (request.Files.Any(f => f.Length > MaxBytes))
            return Error.Validation("Files", "Максимальный размер файла — 10 МБ.");

        var ticket = SupportTicket.Create(currentUser.Id, request.Title, request.Description, request.ContactInfo);

        if (request.Files.Count > 0)
        {
            await storage.EnsureBucketExistsAsync(Bucket, ct);

            foreach (var file in request.Files)
            {
                var objectName = $"{ticket.Id.Value}/{Guid.NewGuid():N}-{file.FileName}";
                var url = await storage.UploadAsync(Bucket, objectName, file.Content, file.ContentType, ct);
                ticket.AddAttachment(url, file.FileName, file.ContentType);
            }
        }

        await tickets.AddAsync(ticket, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return ticket.Id.Value;
    }
}
