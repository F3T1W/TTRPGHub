using FluentValidation;

namespace TTRPGHub.Features.Homebrew.Commands.CreateHomebrew;

internal sealed class CreateHomebrewCommandValidator : AbstractValidator<CreateHomebrewCommand>
{
    public CreateHomebrewCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(500);
        RuleFor(x => x.System).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Content).NotEmpty();
    }
}
