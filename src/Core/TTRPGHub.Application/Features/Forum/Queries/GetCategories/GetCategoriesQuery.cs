using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.Forum.Queries.GetCategories;

public sealed record GetCategoriesQuery : IRequest<Result<List<ForumCategoryDto>>>;

public sealed record ForumCategoryDto(
    Guid Id,
    string Name,
    string Description,
    string Slug,
    int DisplayOrder,
    int TopicCount);
