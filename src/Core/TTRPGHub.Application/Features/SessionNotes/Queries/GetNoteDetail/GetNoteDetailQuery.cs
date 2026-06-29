using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.SessionNotes.Queries.GetNoteDetail;

public sealed record GetNoteDetailQuery(Guid NoteId) : IRequest<Result<SessionNoteDetailDto>>;

public sealed record SessionNoteDetailDto(
    Guid Id, Guid CampaignId, Guid AuthorId, string AuthorName,
    string Title, string Content, DateTime SessionDate,
    DateTime CreatedAt, DateTime UpdatedAt, bool IsAuthor);
