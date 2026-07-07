using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Features.GameTable.Shared;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.GameTable.Commands.CreateJournalEntry;

internal sealed class CreateJournalEntryCommandHandler(
    IGameSessionRepository sessionRepository,
    IJournalEntryRepository journalRepository,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser
) : IRequestHandler<CreateJournalEntryCommand, Result<JournalEntryDto>>
{
    public async Task<Result<JournalEntryDto>> Handle(CreateJournalEntryCommand command, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(command.Title))
            return Error.Validation("Journal.Title", "Заголовок обязателен.");

        var session = await sessionRepository.GetByIdAsync(new GameSessionId(command.SessionId), ct);
        if (session is null)
            return Error.NotFound(nameof(GameSession));

        if (session.OrganizerId != currentUser.Id)
            return Error.Unauthorized();

        // Новая запись всегда черновик (см. JournalEntry.Create) — не рассылаем по SignalR
        // в общую группу сессии, иначе игроки получат ещё неопубликованный текст в браузере
        // раньше, чем GM успеет его отредактировать/опубликовать.
        var entry = JournalEntry.Create(
            session.Id, currentUser.Id, command.Title.Trim(), command.ContentMarkdown,
            command.ParentId,
            command.CampaignId is { } cid ? new CampaignId(cid) : null);
        await journalRepository.AddAsync(entry, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return JournalEntryMapper.ToDto(entry);
    }
}
