using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.Modules.Commands.ImportModule;

public sealed record ImportModuleCommand(string ManifestJson) : IRequest<Result<ImportModuleResponse>>;

public sealed record ImportModuleResponse(int MacrosImported, int RuleEntriesImported, string? SystemSlug);
