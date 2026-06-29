using FluentValidation;

namespace TTRPGHub.Features.Characters.Commands.UpdateCharacter;

internal sealed class UpdateCharacterCommandValidator : AbstractValidator<UpdateCharacterCommand>
{
    public UpdateCharacterCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Race).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Class).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Level).InclusiveBetween(1, 20);
        RuleFor(x => x.ExperiencePoints).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Strength).InclusiveBetween(1, 30);
        RuleFor(x => x.Dexterity).InclusiveBetween(1, 30);
        RuleFor(x => x.Constitution).InclusiveBetween(1, 30);
        RuleFor(x => x.Intelligence).InclusiveBetween(1, 30);
        RuleFor(x => x.Wisdom).InclusiveBetween(1, 30);
        RuleFor(x => x.Charisma).InclusiveBetween(1, 30);
        RuleFor(x => x.MaxHitPoints).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ArmorClass).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Speed).GreaterThanOrEqualTo(0);
    }
}
