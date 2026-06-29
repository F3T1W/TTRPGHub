using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using TTRPGHub.Services;

namespace TTRPGHub.Pages.Homebrew;

public partial class Detail
{
    [Parameter] public Guid Id { get; set; }
    [Inject] private IApiClient Api { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;
    [Inject] private TokenStorage Tokens { get; set; } = default!;

    private HomebrewDetailDto? _item;
    private bool _loading = true;
    private bool _isOwner;
    private int _likeCount;

    protected override async Task OnParametersSetAsync()
    {
        _loading = true;
        _item = await Api.GetHomebrewDetailAsync(Id);
        _likeCount = _item.LikeCount;

        var userId = await Tokens.GetUserIdAsync();
        _isOwner = userId.HasValue && userId.Value == _item.AuthorId;
        _loading = false;
    }

    private async Task ToggleLikeAsync()
    {
        var result = await Api.ToggleHomebrewLikeAsync(Id);
        _likeCount = result.LikeCount;
        _item = _item! with { LikedByMe = result.Liked };
    }

    private async Task DeleteAsync()
    {
        await Api.DeleteHomebrewAsync(Id);
        Nav.NavigateTo("/homebrew");
    }

    private static string TypeLabel(string type) =>
        Enum.TryParse<HomebrewType>(type, out var t)
            ? t switch
            {
                HomebrewType.Spell => "Заклинание",
                HomebrewType.Monster => "Существо",
                HomebrewType.Class => "Класс",
                HomebrewType.Subclass => "Подкласс",
                HomebrewType.Race => "Раса",
                HomebrewType.Subrace => "Подраса",
                HomebrewType.Item => "Предмет",
                HomebrewType.Background => "Предыстория",
                HomebrewType.Feat => "Черта",
                _ => "Другое"
            }
            : type;
}
