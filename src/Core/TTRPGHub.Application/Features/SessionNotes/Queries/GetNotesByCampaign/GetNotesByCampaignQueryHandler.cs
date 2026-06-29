using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Entities;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.SessionNotes.Queries.GetNotesByCampaign;

internal sealed class GetNotesByCampaignQueryHandler(
    ISessionNoteRepository noteRepository,
    IUserRepository userRepository
) : IRequestHandler<GetNotesByCampaignQuery, Result<IReadOnlyList<SessionNoteSummaryDto>>>
{
    public async Task<Result<IReadOnlyList<SessionNoteSummaryDto>>> Handle(
        GetNotesByCampaignQuery query, CancellationToken ct)
    {
        var notes = await noteRepository.GetByCampaignAsync(new CampaignId(query.CampaignId), ct);

        var authorIds = notes.Select(n => n.AuthorId).Distinct().ToList();
        var authors = new Dictionary<UserId, string>();
        foreach (var id in authorIds)
        {
            var user = await userRepository.GetByIdAsync(id, ct);
            if (user is not null) authors[id] = user.Username;
        }

        IReadOnlyList<SessionNoteSummaryDto> result = notes
            .OrderByDescending(n => n.SessionDate)
            .Select(n => new SessionNoteSummaryDto(
                n.Id.Value, n.CampaignId.Value, n.AuthorId.Value,
                authors.GetValueOrDefault(n.AuthorId, "Неизвестно"),
                n.Title, n.SessionDate, n.UpdatedAt))
            .ToList();

        return Result<IReadOnlyList<SessionNoteSummaryDto>>.Success(result);
    }
}
