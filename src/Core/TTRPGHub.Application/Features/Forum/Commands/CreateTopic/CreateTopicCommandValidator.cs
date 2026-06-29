using FluentValidation;

namespace TTRPGHub.Features.Forum.Commands.CreateTopic;

internal sealed class CreateTopicCommandValidator : AbstractValidator<CreateTopicCommand>
{
    public CreateTopicCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.FirstPostContent).NotEmpty().MaximumLength(10000);
    }
}
