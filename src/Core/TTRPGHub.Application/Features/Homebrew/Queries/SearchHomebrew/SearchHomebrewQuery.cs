using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Entities.Homebrew;
using TTRPGHub.Features.Forum.Queries.GetTopics;

namespace TTRPGHub.Features.Homebrew.Queries.SearchHomebrew;

public sealed record SearchHomebrewQuery(
    string? Query = null,
    string? System = null,
    HomebrewType? Type = null,
    string? Tag = null,
    int Page = 1,
    int PageSize = 20)
    : IRequest<Result<PagedResult<HomebrewItemDto>>>;

public sealed record HomebrewItemDto(
    Guid Id,
    string Title,
    string Description,
    string System,
    string Type,
    string Tags,
    Guid AuthorId,
    string AuthorUsername,
    int LikeCount,
    bool LikedByMe,
    DateTime CreatedAt);
