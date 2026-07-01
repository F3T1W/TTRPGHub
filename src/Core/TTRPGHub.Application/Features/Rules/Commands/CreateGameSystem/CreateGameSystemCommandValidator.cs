using FluentValidation;

namespace TTRPGHub.Features.Rules.Commands.CreateGameSystem;

internal sealed class CreateGameSystemCommandValidator : AbstractValidator<CreateGameSystemCommand>
{
    public CreateGameSystemCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
    }
}
