using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.SessionNotes.Commands.DeleteNote;

public sealed record DeleteNoteCommand(Guid NoteId) : IRequest<Result>;
