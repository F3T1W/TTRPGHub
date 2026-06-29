using FluentValidation;

namespace TTRPGHub.Features.Events.Commands.CreateEvent;

internal sealed class CreateEventCommandValidator : AbstractValidator<CreateEventCommand>
{
    public CreateEventCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.System).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Format).NotEmpty();
        RuleFor(x => x.StartsAt).GreaterThan(DateTime.UtcNow).WithMessage("Дата события должна быть в будущем.");
        RuleFor(x => x.MaxParticipants).InclusiveBetween(1, 500);
        RuleFor(x => x.Description).MaximumLength(3000).When(x => x.Description is not null);
        RuleFor(x => x.Location).MaximumLength(300).When(x => x.Location is not null);
        RuleFor(x => x.OnlineLink).MaximumLength(500).When(x => x.OnlineLink is not null);
    }
}
