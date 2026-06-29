using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.SessionNotes.Queries.GetNoteDetail;

internal sealed class GetNoteDetailQueryHandler(
    ISessionNoteRepository noteRepository,
    IUserRepository userRepository,
    ICurrentUser currentUser
) : IRequestHandler<GetNoteDetailQuery, Result<SessionNoteDetailDto>>
{
    public async Task<Result<SessionNoteDetailDto>> Handle(GetNoteDetailQuery query, CancellationToken ct)
    {
        var note = await noteRepository.GetByIdAsync(new SessionNoteId(query.NoteId), ct);
        if (note is null) return Error.NotFound(nameof(SessionNote));

        var author = await userRepository.GetByIdAsync(note.AuthorId, ct);
        return Result<SessionNoteDetailDto>.Success(new SessionNoteDetailDto(
            note.Id.Value, note.CampaignId.Value, note.AuthorId.Value,
            author?.Username ?? "Неизвестно",
            note.Title, note.Content, note.SessionDate,
            note.CreatedAt, note.UpdatedAt,
            note.AuthorId == currentUser.Id));
    }
}
