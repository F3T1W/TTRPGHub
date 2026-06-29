using FluentValidation;

namespace TTRPGHub.Features.Forum.Commands.CreatePost;

internal sealed class CreatePostCommandValidator : AbstractValidator<CreatePostCommand>
{
    public CreatePostCommandValidator()
    {
        RuleFor(x => x.Content).NotEmpty().MaximumLength(10000);
    }
}
