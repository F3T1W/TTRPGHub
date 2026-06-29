using FluentValidation;

namespace TTRPGHub.Features.Characters.Commands.CreateCharacter;

internal sealed class CreateCharacterCommandValidator : AbstractValidator<CreateCharacterCommand>
{
    public CreateCharacterCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Имя персонажа обязательно.")
            .MaximumLength(64).WithMessage("Максимум 64 символа.");

        RuleFor(x => x.Race)
            .NotEmpty().WithMessage("Раса обязательна.");

        RuleFor(x => x.Class)
            .NotEmpty().WithMessage("Класс обязателен.");

        RuleFor(x => x.Level)
            .InclusiveBetween(1, 20).WithMessage("Уровень должен быть от 1 до 20.");
    }
}
