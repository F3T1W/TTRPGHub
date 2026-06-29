using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Features.Forum.Queries.GetTopics;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.Homebrew.Queries.SearchHomebrew;

internal sealed class SearchHomebrewQueryHandler(
    IHomebrewRepository homebrew,
    ICurrentUser currentUser)
    : IRequestHandler<SearchHomebrewQuery, Result<PagedResult<HomebrewItemDto>>>
{
    public async Task<Result<PagedResult<HomebrewItemDto>>> Handle(SearchHomebrewQuery request, CancellationToken ct)
    {
        var (items, total) = await homebrew.SearchAsync(
            request.Query, request.System, request.Type, request.Tag,
            request.Page, request.PageSize, ct);

        var userId = currentUser.IsAuthenticated ? currentUser.Id : (Entities.UserId?)null;

        var dtos = items.Select(i => new HomebrewItemDto(
            i.Id.Value,
            i.Title,
            i.Description,
            i.System,
            i.Type.ToString(),
            i.Tags,
            i.AuthorId.Value,
            i.Author.Username,
            i.Likes.Count,
            userId.HasValue && i.Likes.Any(l => l.UserId == userId.Value),
            i.CreatedAt))
            .ToList();

        return Result<PagedResult<HomebrewItemDto>>.Success(
            new PagedResult<HomebrewItemDto>(dtos, total, request.Page, request.PageSize));
    }
}
