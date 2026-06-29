using FluentValidation;

namespace TTRPGHub.Features.Sessions.Commands.UpdateSession;

internal sealed class UpdateSessionCommandValidator : AbstractValidator<UpdateSessionCommand>
{
    public UpdateSessionCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.System).NotEmpty().MaximumLength(100);
        RuleFor(x => x.MaxPlayers).InclusiveBetween(2, 10);
    }
}
