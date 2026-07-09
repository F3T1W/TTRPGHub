using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Features.Macros.Shared;

namespace TTRPGHub.Features.Macros.Queries.GetMyMacros;

public sealed record GetMyMacrosQuery : IRequest<Result<List<MacroDto>>>;
