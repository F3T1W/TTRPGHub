using FluentValidation;

namespace TTRPGHub.Features.Tickets.Commands.CreateTicket;

internal sealed class CreateTicketCommandValidator : AbstractValidator<CreateTicketCommand>
{
    public CreateTicketCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(5000);
        RuleFor(x => x.ContactInfo).MaximumLength(300).When(x => x.ContactInfo is not null);
        RuleFor(x => x.Files).Must(f => f.Count <= 5).WithMessage("Не более 5 вложений на тикет.");
    }
}
