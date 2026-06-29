using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Repositories.Forum;

namespace TTRPGHub.Features.Forum.Queries.GetCategories;

internal sealed class GetCategoriesQueryHandler(IForumCategoryRepository categories)
    : IRequestHandler<GetCategoriesQuery, Result<List<ForumCategoryDto>>>
{
    public async Task<Result<List<ForumCategoryDto>>> Handle(GetCategoriesQuery request, CancellationToken ct)
    {
        var list = await categories.GetAllAsync(ct);
        var dtos = list
            .OrderBy(c => c.DisplayOrder)
            .Select(c => new ForumCategoryDto(
                c.Id.Value,
                c.Name,
                c.Description,
                c.Slug,
                c.DisplayOrder,
                c.Topics.Count))
            .ToList();

        return Result<List<ForumCategoryDto>>.Success(dtos);
    }
}
