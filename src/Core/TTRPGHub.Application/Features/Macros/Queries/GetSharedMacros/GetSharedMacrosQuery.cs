using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Features.Macros.Shared;

namespace TTRPGHub.Features.Macros.Queries.GetSharedMacros;

public sealed record GetSharedMacrosQuery(Guid SessionId) : IRequest<Result<List<MacroDto>>>;
