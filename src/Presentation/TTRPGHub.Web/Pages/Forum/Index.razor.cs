using Microsoft.AspNetCore.Components;
using TTRPGHub.Services;

namespace TTRPGHub.Pages.Forum;

public partial class Index
{
    [Inject] private IApiClient Api { get; set; } = default!;

    private List<ForumCategoryDto>? _categories;
    private bool _loading = true;

    protected override async Task OnInitializedAsync()
    {
        _categories = await Api.GetForumCategoriesAsync();
        _loading = false;
    }
}
