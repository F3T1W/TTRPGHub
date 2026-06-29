using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.SessionNotes.Queries.GetNotesByCampaign;

public sealed record GetNotesByCampaignQuery(Guid CampaignId) : IRequest<Result<IReadOnlyList<SessionNoteSummaryDto>>>;

public sealed record SessionNoteSummaryDto(
    Guid Id, Guid CampaignId, Guid AuthorId, string AuthorName,
    string Title, DateTime SessionDate, DateTime UpdatedAt);
