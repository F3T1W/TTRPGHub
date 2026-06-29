using FluentValidation;

namespace TTRPGHub.Features.Sessions.Commands.CreateSession;

internal sealed class CreateSessionCommandValidator : AbstractValidator<CreateSessionCommand>
{
    public CreateSessionCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.System).NotEmpty().MaximumLength(100);
        RuleFor(x => x.MaxPlayers).InclusiveBetween(2, 10);
        RuleFor(x => x.ScheduledAt).GreaterThan(DateTime.UtcNow)
            .WithMessage("Дата сессии должна быть в будущем.");
    }
}
