using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using TTRPGHub.Services;

namespace TTRPGHub.Pages.Users;

public partial class Profile
{
    [Parameter] public Guid Id { get; set; }
    [Inject] private IApiClient Api { get; set; } = default!;
    [Inject] private TokenStorage Tokens { get; set; } = default!;

    private UserProfileDto? _profile;
    private UserRatingsResult? _ratings;
    private UserSessionReviewsResult? _sessionReviews;
    private bool _loading = true;

    private bool _isOwn;
    private bool _showRatingForm;
    private int _formScore = 5;
    private string _formComment = string.Empty;
    private string _formRole = "Player";
    private string? _formError;
    private bool _submitting;
    private string? _actionMessage;

    private bool _editingProfile;
    private string _editDisplayName = string.Empty;
    private string _editBio = string.Empty;
    private string _editCity = string.Empty;
    private string? _editProfileError;
    private bool _savingProfile;

    protected override async Task OnParametersSetAsync()
    {
        _loading = true;
        try
        {
            _profile = await Api.GetUserProfileAsync(Id);
            _ratings = await Api.GetUserRatingsAsync(Id);
            _sessionReviews = await Api.GetUserSessionReviewsAsync(Id);

            var myId = await Tokens.GetUserIdAsync();
            _isOwn = myId.HasValue && myId.Value == Id;
        }
        catch { _profile = null; }
        finally { _loading = false; }
    }

    private async Task SubmitRatingAsync()
    {
        _submitting = true;
        _formError = null;
        try
        {
            await Api.RateUserAsync(Id, new RateUserRequest(_formScore, _formComment, _formRole));
            _ratings = await Api.GetUserRatingsAsync(Id);
            _showRatingForm = false;
            _formComment = string.Empty;
            _formScore = 5;
        }
        catch (Exception ex)
        {
            _formError = ex.Message;
        }
        finally { _submitting = false; }
    }

    private async Task DeleteRatingAsync(Guid ratingId)
    {
        try
        {
            await Api.DeleteRatingAsync(ratingId);
            _ratings = await Api.GetUserRatingsAsync(Id);
        }
        catch { }
    }

    private void StartEditProfile()
    {
        if (_profile is null) return;
        _editDisplayName = _profile.DisplayName ?? string.Empty;
        _editBio = _profile.Bio ?? string.Empty;
        _editCity = _profile.City ?? string.Empty;
        _editProfileError = null;
        _editingProfile = true;
    }

    private async Task SaveProfileAsync()
    {
        _savingProfile = true;
        _editProfileError = null;
        try
        {
            await Api.UpdateProfileAsync(new UpdateProfileRequest(
                string.IsNullOrWhiteSpace(_editDisplayName) ? null : _editDisplayName,
                string.IsNullOrWhiteSpace(_editBio) ? null : _editBio,
                string.IsNullOrWhiteSpace(_editCity) ? null : _editCity));
            _editingProfile = false;
            _profile = await Api.GetUserProfileAsync(Id);
        }
        catch
        {
            _editProfileError = "Не удалось сохранить профиль.";
        }
        finally
        {
            _savingProfile = false;
        }
    }

    private async Task ReportRatingAsync(Guid ratingId)
    {
        try
        {
            await Api.CreateReportAsync(new CreateReportRequest("Rating", ratingId, "Жалоба на отзыв"));
            _actionMessage = "Жалоба отправлена, модераторы её рассмотрят.";
        }
        catch
        {
            _actionMessage = "Не удалось отправить жалобу.";
        }
    }

    private static string StarClass(int star, int score) =>
        star <= score ? "bi-star-fill" : "bi-star";

    private static string RoleLabel(string role) =>
        role == "DungeonMaster" ? "Мастер подземелий" : "Игрок";
}
