using FluentValidation;

namespace TTRPGHub.Features.Auth.Commands.Register;

internal sealed class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Имя пользователя обязательно.")
            .MinimumLength(3).WithMessage("Минимум 3 символа.")
            .MaximumLength(32).WithMessage("Максимум 32 символа.")
            .Matches("^[a-zA-Z0-9_]+$").WithMessage("Только буквы, цифры и _.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email обязателен.")
            .EmailAddress().WithMessage("Некорректный формат email.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Пароль обязателен.")
            .MinimumLength(8).WithMessage("Минимум 8 символов.")
            .Matches("[A-Z]").WithMessage("Хотя бы одна заглавная буква.")
            .Matches("[0-9]").WithMessage("Хотя бы одна цифра.");
    }
}
