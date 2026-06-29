using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.SessionNotes.Commands.CreateNote;

public sealed record CreateNoteCommand(Guid CampaignId, string Title, string Content, DateTime SessionDate)
    : IRequest<Result<CreateNoteResponse>>;

public sealed record CreateNoteResponse(Guid NoteId);
