using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.Homebrew.Queries.GetHomebrewDetail;

public sealed record GetHomebrewDetailQuery(Guid Id) : IRequest<Result<HomebrewDetailDto>>;

public sealed record HomebrewDetailDto(
    Guid Id,
    string Title,
    string Description,
    string System,
    string Type,
    string Content,
    string Tags,
    Guid AuthorId,
    string AuthorUsername,
    int LikeCount,
    bool LikedByMe,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
