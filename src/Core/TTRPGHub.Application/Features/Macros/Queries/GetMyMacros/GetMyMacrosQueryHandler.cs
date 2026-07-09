using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Features.Macros.Shared;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.Macros.Queries.GetMyMacros;

internal sealed class GetMyMacrosQueryHandler(
    IMacroRepository macroRepository,
    ICurrentUser currentUser
) : IRequestHandler<GetMyMacrosQuery, Result<List<MacroDto>>>
{
    public async Task<Result<List<MacroDto>>> Handle(GetMyMacrosQuery query, CancellationToken ct)
    {
        var macros = await macroRepository.GetByOwnerAsync(currentUser.Id, ct);
        return macros.Select(MacroMapper.ToDto).ToList();
    }
}
