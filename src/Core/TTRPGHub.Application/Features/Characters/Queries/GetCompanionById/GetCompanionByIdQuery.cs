using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Features.Characters.Queries.GetCompanions;

namespace TTRPGHub.Features.Characters.Queries.GetCompanionById;

// N.8 — используется со стола (Table.razor.cs), где известен только CompanionId выбранного
// токена, а не CharacterId владельца (в отличие от GetCompanionsQuery — там список по владельцу).
public sealed record GetCompanionByIdQuery(Guid CompanionId) : IRequest<Result<CompanionDto>>;
