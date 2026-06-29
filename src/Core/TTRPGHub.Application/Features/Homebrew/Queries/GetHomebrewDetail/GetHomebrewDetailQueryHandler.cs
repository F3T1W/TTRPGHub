using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities.Homebrew;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.Homebrew.Queries.GetHomebrewDetail;

internal sealed class GetHomebrewDetailQueryHandler(
    IHomebrewRepository homebrew,
    ICurrentUser currentUser)
    : IRequestHandler<GetHomebrewDetailQuery, Result<HomebrewDetailDto>>
{
    public async Task<Result<HomebrewDetailDto>> Handle(GetHomebrewDetailQuery request, CancellationToken ct)
    {
        var item = await homebrew.GetByIdAsync(HomebrewItemId.From(request.Id), ct);
        if (item is null)
            return Error.NotFound(nameof(item));

        var userId = currentUser.IsAuthenticated ? currentUser.Id : (Entities.UserId?)null;

        return Result<HomebrewDetailDto>.Success(new HomebrewDetailDto(
            item.Id.Value,
            item.Title,
            item.Description,
            item.System,
            item.Type.ToString(),
            item.Content,
            item.Tags,
            item.AuthorId.Value,
            item.Author.Username,
            item.Likes.Count,
            userId.HasValue && item.Likes.Any(l => l.UserId == userId.Value),
            item.CreatedAt,
            item.UpdatedAt));
    }
}
