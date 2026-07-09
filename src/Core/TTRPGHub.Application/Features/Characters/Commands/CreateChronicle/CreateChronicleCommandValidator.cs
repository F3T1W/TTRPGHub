using FluentValidation;

namespace TTRPGHub.Features.Characters.Commands.CreateChronicle;

internal sealed class CreateChronicleCommandValidator : AbstractValidator<CreateChronicleCommand>
{
    public CreateChronicleCommandValidator()
    {
        RuleFor(x => x.ScenarioName)
            .NotEmpty().WithMessage("Название сценария обязательно.")
            .MaximumLength(200).WithMessage("Максимум 200 символов.");

        RuleFor(x => x.GoldEarned).GreaterThanOrEqualTo(0);
        RuleFor(x => x.AchievementPoints).GreaterThanOrEqualTo(0);
    }
}
