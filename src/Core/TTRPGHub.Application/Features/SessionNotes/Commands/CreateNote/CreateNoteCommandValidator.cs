using FluentValidation;

namespace TTRPGHub.Features.SessionNotes.Commands.CreateNote;

internal sealed class CreateNoteCommandValidator : AbstractValidator<CreateNoteCommand>
{
    public CreateNoteCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Content).NotEmpty().MaximumLength(50000);
        RuleFor(x => x.CampaignId).NotEmpty();
        RuleFor(x => x.SessionDate).NotEmpty();
    }
}
