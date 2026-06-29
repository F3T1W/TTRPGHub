using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.SessionNotes.Commands.UpdateNote;

public sealed record UpdateNoteCommand(Guid NoteId, string Title, string Content, DateTime SessionDate)
    : IRequest<Result>;
