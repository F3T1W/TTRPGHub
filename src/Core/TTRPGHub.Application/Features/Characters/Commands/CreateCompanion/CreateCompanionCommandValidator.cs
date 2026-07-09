using FluentValidation;

namespace TTRPGHub.Features.Characters.Commands.CreateCompanion;

internal sealed class CreateCompanionCommandValidator : AbstractValidator<CreateCompanionCommand>
{
    public CreateCompanionCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Имя компаньона обязательно.")
            .MaximumLength(200).WithMessage("Максимум 200 символов.");

        RuleFor(x => x.Level).InclusiveBetween(1, 20).WithMessage("Уровень должен быть от 1 до 20.");
        RuleFor(x => x.MaxHitPoints).GreaterThanOrEqualTo(0);
    }
}
