using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.SessionNotes.Commands.UpdateNote;

internal sealed class UpdateNoteCommandHandler(
    ISessionNoteRepository noteRepository,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser
) : IRequestHandler<UpdateNoteCommand, Result>
{
    public async Task<Result> Handle(UpdateNoteCommand command, CancellationToken ct)
    {
        var note = await noteRepository.GetByIdAsync(new SessionNoteId(command.NoteId), ct);
        if (note is null) return Error.NotFound(nameof(SessionNote));
        if (note.AuthorId != currentUser.Id) return Error.Unauthorized();

        note.Update(command.Title, command.Content, command.SessionDate);
        noteRepository.Update(note);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }
}
