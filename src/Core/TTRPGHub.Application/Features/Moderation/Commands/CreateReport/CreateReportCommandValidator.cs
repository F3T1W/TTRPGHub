using FluentValidation;

namespace TTRPGHub.Features.Moderation.Commands.CreateReport;

internal sealed class CreateReportCommandValidator : AbstractValidator<CreateReportCommand>
{
    public CreateReportCommandValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(1000);
    }
}
