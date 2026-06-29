using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.SessionNotes.Commands.DeleteNote;

internal sealed class DeleteNoteCommandHandler(
    ISessionNoteRepository noteRepository,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser
) : IRequestHandler<DeleteNoteCommand, Result>
{
    public async Task<Result> Handle(DeleteNoteCommand command, CancellationToken ct)
    {
        var note = await noteRepository.GetByIdAsync(new SessionNoteId(command.NoteId), ct);
        if (note is null) return Error.NotFound(nameof(SessionNote));
        if (note.AuthorId != currentUser.Id) return Error.Unauthorized();

        noteRepository.Delete(note);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }
}
