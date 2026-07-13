using FluentValidation;

namespace TTRPGHub.Features.Ratings.Commands.RateSessionParticipant;

internal sealed class RateSessionParticipantCommandValidator : AbstractValidator<RateSessionParticipantCommand>
{
    public RateSessionParticipantCommandValidator()
    {
        RuleFor(x => x.Score).InclusiveBetween(1, 5).WithMessage("Оценка должна быть от 1 до 5.");
        RuleFor(x => x.Comment).MaximumLength(1000).When(x => x.Comment is not null);
    }
}
