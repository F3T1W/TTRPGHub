using Microsoft.AspNetCore.Components;
using TTRPGHub.Services;

namespace TTRPGHub.Pages.Forum;

public partial class Category
{
    [Parameter] public string Slug { get; set; } = default!;
    [Inject] private IApiClient Api { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;

    private ForumTopicsPagedResult? _data;
    private string? _categoryName;
    private bool _loading = true;
    private int _page = 1;

    private bool _showCreate;
    private string _newTitle = "";
    private string _newContent = "";
    private string? _createError;

    private Guid _categoryId;

    protected override async Task OnParametersSetAsync()
    {
        await LoadPageAsync(1);
    }

    private async Task LoadPageAsync(int page)
    {
        _loading = true;
        _page = page;

        var categoriesTask = _categoryName is null ? Api.GetForumCategoriesAsync() : Task.FromResult<List<ForumCategoryDto>>(null!);
        _data = await Api.GetForumTopicsAsync(Slug, page, 20);

        if (_categoryName is null)
        {
            var categories = await categoriesTask;
            var cat = categories.FirstOrDefault(c => c.Slug == Slug);
            _categoryName = cat?.Name ?? Slug;
            _categoryId = cat?.Id ?? Guid.Empty;
        }

        _loading = false;
    }

    private async Task CreateTopicAsync()
    {
        _createError = null;
        if (string.IsNullOrWhiteSpace(_newTitle) || string.IsNullOrWhiteSpace(_newContent))
        {
            _createError = "Заполни заголовок и текст";
            return;
        }

        try
        {
            var id = await Api.CreateForumTopicAsync(new CreateForumTopicRequest(_categoryId, _newTitle, _newContent));
            Nav.NavigateTo($"/forum/topic/{id}");
        }
        catch
        {
            _createError = "Не удалось создать тему";
        }
    }
}
