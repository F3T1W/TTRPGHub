using FluentValidation;

namespace TTRPGHub.Features.Encounters.Commands.CreateEncounter;

internal sealed class CreateEncounterCommandValidator : AbstractValidator<CreateEncounterCommand>
{
    public CreateEncounterCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.CampaignId).NotEmpty();
        RuleFor(x => x.Entries).NotNull();
        RuleForEach(x => x.Entries).ChildRules(e =>
        {
            e.RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
            e.RuleFor(x => x.Count).GreaterThan(0);
        });
    }
}
