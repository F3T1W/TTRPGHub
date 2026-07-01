using System.Text.Json;
using FluentValidation;

namespace TTRPGHub.Features.Rules.Commands.CreateRuleEntry;

internal sealed class CreateRuleEntryCommandValidator : AbstractValidator<CreateRuleEntryCommand>
{
    public CreateRuleEntryCommandValidator()
    {
        RuleFor(x => x.SystemSlug).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Summary).MaximumLength(500).When(x => x.Summary is not null);
        RuleFor(x => x.ContentMarkdown).MaximumLength(20000).When(x => x.ContentMarkdown is not null);
        RuleFor(x => x.StatsJson).Must(BeValidJson).WithMessage("StatsJson должен быть корректным JSON-объектом.");
    }

    private static bool BeValidJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return true;
        try
        {
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.ValueKind == JsonValueKind.Object;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
