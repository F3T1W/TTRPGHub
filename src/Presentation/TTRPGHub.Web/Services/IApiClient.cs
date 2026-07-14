using Refit;

namespace TTRPGHub.Services;

// ── Auth ─────────────────────────────────────────────────────────────────────

public sealed record RegisterRequest(string Username, string Email, string Password);
public sealed record RegisterResponse(Guid UserId, string Username, string Email);

public sealed record LoginRequest(string Email, string Password);
public sealed record LoginResponse(string AccessToken, string RefreshToken, string Username, Guid UserId);

public sealed record ConfirmEmailRequest(string Token);
public sealed record ForgotPasswordRequest(string Email);
public sealed record ResetPasswordRequest(string Token, string NewPassword);

// ── Characters ────────────────────────────────────────────────────────────────

public sealed record CreateCharacterRequest(string Name, string Race, string Class, int Level);

public sealed record CreateCharacterFromRulesRequest(
    string Name, string SystemSlug, string RaceSlug, string ClassSlug, int Level,
    int Strength, int Dexterity, int Constitution, int Intelligence, int Wisdom, int Charisma);

public sealed record LevelUpRequest(int NewLevel);
public sealed record LevelUpResponse(Guid CharacterId, int Level, int MaxHitPoints, int CurrentHitPoints, string? WhatsNew);

public sealed record CreateCharacterFromRulesResponse(
    Guid CharacterId, string Name, string Race, string Class, int Level,
    int Strength, int Dexterity, int Constitution, int Intelligence, int Wisdom, int Charisma,
    int MaxHitPoints, int ArmorClass, List<string> SavingThrowProficiencies, string? ProficiencyNotes);
public sealed record CreateCharacterResponse(Guid CharacterId, string Name, string Race, string Class, int Level);
public sealed record ImportCharacterResponse(Guid CharacterId, string Name);
public sealed record ImportCharacterRequest(
    string Name, string Race, string Class, int Level,
    bool IsPublic = false,
    string? Background = null, string? Alignment = null, int ExperiencePoints = 0,
    string? PersonalityTraits = null, string? Ideals = null, string? Bonds = null, string? Flaws = null,
    int Strength = 10, int Dexterity = 10, int Constitution = 10,
    int Intelligence = 10, int Wisdom = 10, int Charisma = 10,
    int MaxHitPoints = 1, int CurrentHitPoints = 1, int TemporaryHitPoints = 0,
    int ArmorClass = 10, int Speed = 30, string HitDice = "1d8",
    List<string>? SkillProficiencies = null, List<string>? SavingThrowProficiencies = null,
    string? FeaturesAndTraits = null, string? Equipment = null);

public sealed record CharacterSummaryDto(
    Guid Id, string Name, string Race, string Class,
    int Level, string? AvatarUrl, DateTime UpdatedAt);

public sealed record CharacterDetailDto(
    Guid Id,
    Guid OwnerId,
    string Name,
    string Race,
    string Class,
    int Level,
    bool IsPublic,
    string? Background,
    string? Alignment,
    int ExperiencePoints,
    string? PersonalityTraits,
    string? Ideals,
    string? Bonds,
    string? Flaws,
    int Strength,
    int Dexterity,
    int Constitution,
    int Intelligence,
    int Wisdom,
    int Charisma,
    int StrengthModifier,
    int DexterityModifier,
    int ConstitutionModifier,
    int IntelligenceModifier,
    int WisdomModifier,
    int CharismaModifier,
    int ProficiencyBonus,
    int Initiative,
    int MaxHitPoints,
    int CurrentHitPoints,
    int TemporaryHitPoints,
    int ArmorClass,
    int Speed,
    string HitDice,
    List<string> SkillProficiencies,
    List<string> SavingThrowProficiencies,
    string? FeaturesAndTraits,
    string? Equipment,
    string? AvatarUrl,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    string? Pf2eStatsJson,
    string? SelectedFeatsJson,
    List<CoOwnerDto> CoOwners
);

public sealed record CoOwnerDto(Guid UserId, string Username);
public sealed record UpdatePf2eStatsRequest(string StatsJson);
public sealed record UpdateFeatsRequest(string SelectedFeatsJson);
public sealed record SelectedFeatDto(string Slug, string Name, int Level);
public sealed record AddCoOwnerRequest(string Username);

// N.3 — Pathfinder Society Chronicle Sheets
public sealed record ChronicleDto(
    Guid Id, string ScenarioName, DateOnly SessionDate, string? GmName, string? Faction,
    int GoldEarned, int AchievementPoints, string? BoonsUsed, string? Notes);

public sealed record CreateChronicleRequest(
    string ScenarioName, DateOnly SessionDate, string? GmName, string? Faction,
    int GoldEarned, int AchievementPoints, string? BoonsUsed, string? Notes);

public sealed record CreateChronicleResponse(Guid ChronicleId);

// N.8 — Companion/Familiar/Animal Companion листы
public sealed record CompanionDto(
    Guid Id, string Name, string Kind, int Level,
    int MaxHitPoints, int CurrentHitPoints, int? ArmorClass,
    string? Speed, string? AttacksText, string? AbilitiesText, string? Notes);

public sealed record CreateCompanionRequest(
    string Name, string Kind, int Level, int MaxHitPoints, int? ArmorClass,
    string? Speed, string? AttacksText, string? AbilitiesText, string? Notes);

public sealed record CreateCompanionResponse(Guid CompanionId);

public sealed record UpdateCompanionRequest(
    string Name, string Kind, int Level, int MaxHitPoints, int CurrentHitPoints, int? ArmorClass,
    string? Speed, string? AttacksText, string? AbilitiesText, string? Notes);

public sealed record UpdateCharacterRequest(
    Guid CharacterId,
    string Name,
    string Race,
    string Class,
    int Level,
    bool IsPublic,
    string? Background,
    string? Alignment,
    int ExperiencePoints,
    string? PersonalityTraits,
    string? Ideals,
    string? Bonds,
    string? Flaws,
    int Strength,
    int Dexterity,
    int Constitution,
    int Intelligence,
    int Wisdom,
    int Charisma,
    int MaxHitPoints,
    int CurrentHitPoints,
    int TemporaryHitPoints,
    int ArmorClass,
    int Speed,
    string HitDice,
    List<string> SkillProficiencies,
    List<string> SavingThrowProficiencies,
    string? FeaturesAndTraits,
    string? Equipment
);

// ── Refit interface ───────────────────────────────────────────────────────────

public interface IApiClient
{
    [Post("/api/auth/register")]
    Task<RegisterResponse> RegisterAsync([Body] RegisterRequest request, CancellationToken ct = default);

    [Post("/api/auth/login")]
    Task<LoginResponse> LoginAsync([Body] LoginRequest request, CancellationToken ct = default);

    [Post("/api/auth/confirm-email")]
    Task ConfirmEmailAsync([Body] ConfirmEmailRequest request, CancellationToken ct = default);

    [Post("/api/auth/forgot-password")]
    Task ForgotPasswordAsync([Body] ForgotPasswordRequest request, CancellationToken ct = default);

    [Post("/api/auth/reset-password")]
    Task ResetPasswordAsync([Body] ResetPasswordRequest request, CancellationToken ct = default);

    [Get("/api/characters/me")]
    Task<List<CharacterSummaryDto>> GetMyCharactersAsync(CancellationToken ct = default);

    [Post("/api/characters")]
    Task<CreateCharacterResponse> CreateCharacterAsync([Body] CreateCharacterRequest request, CancellationToken ct = default);

    [Post("/api/characters/from-rules")]
    Task<CreateCharacterFromRulesResponse> CreateCharacterFromRulesAsync([Body] CreateCharacterFromRulesRequest request, CancellationToken ct = default);

    [Post("/api/characters/{id}/level-up")]
    Task<LevelUpResponse> LevelUpCharacterAsync(Guid id, [Body] LevelUpRequest request, CancellationToken ct = default);

    [Get("/api/characters/{id}")]
    Task<CharacterDetailDto> GetCharacterByIdAsync(Guid id, CancellationToken ct = default);

    [Put("/api/characters/{id}")]
    Task UpdateCharacterAsync(Guid id, [Body] UpdateCharacterRequest request, CancellationToken ct = default);

    [Put("/api/characters/{id}/pf2e-stats")]
    Task UpdatePf2eStatsAsync(Guid id, [Body] UpdatePf2eStatsRequest request, CancellationToken ct = default);

    [Put("/api/characters/{id}/feats")]
    Task UpdateCharacterFeatsAsync(Guid id, [Body] UpdateFeatsRequest request, CancellationToken ct = default);

    [Post("/api/characters/{id}/co-owners")]
    Task AddCoOwnerAsync(Guid id, [Body] AddCoOwnerRequest request, CancellationToken ct = default);

    [Delete("/api/characters/{id}/co-owners/{userId}")]
    Task RemoveCoOwnerAsync(Guid id, Guid userId, CancellationToken ct = default);

    [Get("/api/characters/{id}/chronicles")]
    Task<List<ChronicleDto>> GetChroniclesAsync(Guid id, CancellationToken ct = default);

    [Post("/api/characters/{id}/chronicles")]
    Task<CreateChronicleResponse> CreateChronicleAsync(Guid id, [Body] CreateChronicleRequest request, CancellationToken ct = default);

    [Delete("/api/characters/{id}/chronicles/{chronicleId}")]
    Task DeleteChronicleAsync(Guid id, Guid chronicleId, CancellationToken ct = default);

    [Get("/api/characters/{id}/companions")]
    Task<List<CompanionDto>> GetCompanionsAsync(Guid id, CancellationToken ct = default);

    [Post("/api/characters/{id}/companions")]
    Task<CreateCompanionResponse> CreateCompanionAsync(Guid id, [Body] CreateCompanionRequest request, CancellationToken ct = default);

    [Put("/api/characters/{id}/companions/{companionId}")]
    Task UpdateCompanionAsync(Guid id, Guid companionId, [Body] UpdateCompanionRequest request, CancellationToken ct = default);

    [Delete("/api/characters/{id}/companions/{companionId}")]
    Task DeleteCompanionAsync(Guid id, Guid companionId, CancellationToken ct = default);

    [Get("/api/companions/{companionId}")]
    Task<CompanionDto> GetCompanionByIdAsync(Guid companionId, CancellationToken ct = default);

    [Post("/api/characters/{id}/avatar")]
    [Multipart]
    Task<AvatarUploadResponse> UploadAvatarAsync(Guid id, [AliasAs("file")] StreamPart file, CancellationToken ct = default);

    [Post("/api/characters/import")]
    Task<ImportCharacterResponse> ImportCharacterAsync([Body] ImportCharacterRequest request, CancellationToken ct = default);

    [Get("/api/sessions/upcoming")]
    Task<List<SessionSummaryDto>> GetUpcomingSessionsAsync(
        [Query] int page = 1, [Query] int pageSize = 20,
        [Query] string? location = null, [Query] string? format = null, CancellationToken ct = default);

    [Get("/api/sessions/me")]
    Task<List<SessionSummaryDto>> GetMySessionsAsync(CancellationToken ct = default);

    [Get("/api/sessions/{id}")]
    Task<SessionDetailDto> GetSessionDetailAsync(Guid id, CancellationToken ct = default);

    [Get("/api/sessions/{id}/reviews")]
    Task<List<SessionReviewDto>> GetSessionReviewsAsync(Guid id, CancellationToken ct = default);

    [Post("/api/sessions/{id}/reviews")]
    Task<Guid> RateSessionParticipantAsync(Guid id, [Body] RateSessionParticipantRequest request, CancellationToken ct = default);

    [Get("/api/v1/ratings/session-reviews/user/{userId}")]
    Task<UserSessionReviewsResult> GetUserSessionReviewsAsync(Guid userId, CancellationToken ct = default);

    [Delete("/api/v1/ratings/session-reviews/{reviewId}")]
    Task DeleteSessionReviewAsync(Guid reviewId, CancellationToken ct = default);

    [Post("/api/sessions")]
    Task<CreateSessionResponse> CreateSessionAsync([Body] CreateSessionRequest request, CancellationToken ct = default);

    [Post("/api/sessions/import")]
    Task<ImportSessionResponse> ImportSessionAsync([Body] ImportSessionRequest request, CancellationToken ct = default);

    [Put("/api/sessions/{id}")]
    Task UpdateSessionAsync(Guid id, [Body] UpdateSessionRequest request, CancellationToken ct = default);

    [Post("/api/sessions/{id}/join")]
    Task JoinSessionAsync(Guid id, CancellationToken ct = default);

    [Post("/api/sessions/{id}/leave")]
    Task LeaveSessionAsync(Guid id, CancellationToken ct = default);

    [Patch("/api/sessions/{id}/status")]
    Task ChangeSessionStatusAsync(Guid id, [Body] ChangeStatusRequest request, CancellationToken ct = default);

    // ── Campaigns ────────────────────────────────────────────────────────────

    [Get("/api/v1/campaigns")]
    Task<List<CampaignSummaryDto>> GetAllCampaignsAsync(CancellationToken ct = default);

    [Get("/api/v1/campaigns/me")]
    Task<List<CampaignSummaryDto>> GetMyCampaignsAsync(CancellationToken ct = default);

    [Post("/api/v1/campaigns/import")]
    Task<ImportCampaignResponse> ImportCampaignAsync([Body] ImportCampaignRequest request, CancellationToken ct = default);

    [Get("/api/v1/campaigns/{id}")]
    Task<CampaignDetailDto> GetCampaignDetailAsync(Guid id, CancellationToken ct = default);

    [Post("/api/v1/campaigns")]
    Task<CreateCampaignResponse> CreateCampaignAsync([Body] CreateCampaignRequest request, CancellationToken ct = default);

    [Put("/api/v1/campaigns/{id}")]
    Task UpdateCampaignAsync(Guid id, [Body] UpdateCampaignRequest request, CancellationToken ct = default);

    [Post("/api/v1/campaigns/{id}/participants")]
    Task AddCampaignParticipantAsync(Guid id, [Body] AddParticipantRequest request, CancellationToken ct = default);

    [Delete("/api/v1/campaigns/{id}/participants/{userId}")]
    Task RemoveCampaignParticipantAsync(Guid id, Guid userId, CancellationToken ct = default);

    [Patch("/api/v1/campaigns/{id}/status")]
    Task ChangeCampaignStatusAsync(Guid id, [Body] ChangeCampaignStatusRequest request, CancellationToken ct = default);

    // ── Session Notes ─────────────────────────────────────────────────────────

    [Get("/api/v1/notes/campaign/{campaignId}")]
    Task<List<SessionNoteSummaryDto>> GetNotesByCampaignAsync(Guid campaignId, CancellationToken ct = default);

    [Get("/api/v1/notes/{id}")]
    Task<SessionNoteDetailDto> GetNoteDetailAsync(Guid id, CancellationToken ct = default);

    [Post("/api/v1/notes")]
    Task<CreateNoteResponse> CreateNoteAsync([Body] CreateNoteRequest request, CancellationToken ct = default);

    [Put("/api/v1/notes/{id}")]
    Task UpdateNoteAsync(Guid id, [Body] UpdateNoteRequest request, CancellationToken ct = default);

    [Delete("/api/v1/notes/{id}")]
    Task DeleteNoteAsync(Guid id, CancellationToken ct = default);

    // ── Encounters ────────────────────────────────────────────────────────────

    [Get("/api/v1/encounters/campaign/{campaignId}")]
    Task<List<EncounterSummaryDto>> GetEncountersByCampaignAsync(Guid campaignId, CancellationToken ct = default);

    [Get("/api/v1/encounters/{id}")]
    Task<EncounterDetailDto> GetEncounterDetailAsync(Guid id, CancellationToken ct = default);

    [Post("/api/v1/encounters")]
    Task<CreateEncounterResponse> CreateEncounterAsync([Body] CreateEncounterRequest request, CancellationToken ct = default);

    [Put("/api/v1/encounters/{id}")]
    Task UpdateEncounterAsync(Guid id, [Body] UpdateEncounterRequest request, CancellationToken ct = default);

    [Delete("/api/v1/encounters/{id}")]
    Task DeleteEncounterAsync(Guid id, CancellationToken ct = default);

    // ── Initiative Tracker ────────────────────────────────────────────────────

    [Get("/api/v1/trackers/campaign/{campaignId}")]
    Task<List<TrackerSummaryDto>> GetTrackersByCampaignAsync(Guid campaignId, CancellationToken ct = default);

    [Get("/api/v1/trackers/{id}")]
    Task<TrackerDetailDto> GetTrackerDetailAsync(Guid id, CancellationToken ct = default);

    [Post("/api/v1/trackers/{id}/sync-from-table")]
    Task<TrackerDetailDto> SyncTrackerFromTableAsync(Guid id, [Body] SyncFromTableRequest request, CancellationToken ct = default);

    [Patch("/api/v1/trackers/{id}/link-session")]
    Task<TrackerDetailDto> LinkTrackerSessionAsync(Guid id, [Body] LinkSessionRequest request, CancellationToken ct = default);

    [Post("/api/v1/trackers")]
    Task<CreateTrackerResponse> CreateTrackerAsync([Body] CreateTrackerRequest request, CancellationToken ct = default);

    [Post("/api/v1/trackers/{id}/entries")]
    Task SetTrackerEntriesAsync(Guid id, [Body] List<TrackerEntryInput> entries, CancellationToken ct = default);

    [Patch("/api/v1/trackers/{id}/entries/{entryId}")]
    Task UpdateTrackerEntryAsync(Guid id, Guid entryId, [Body] UpdateEntryRequest request, CancellationToken ct = default);

    [Post("/api/v1/trackers/{id}/start")]
    Task StartTrackerAsync(Guid id, CancellationToken ct = default);

    [Post("/api/v1/trackers/{id}/next")]
    Task NextTurnAsync(Guid id, CancellationToken ct = default);

    [Post("/api/v1/trackers/{id}/previous")]
    Task PreviousTurnAsync(Guid id, CancellationToken ct = default);

    [Delete("/api/v1/trackers/{id}")]
    Task DeleteTrackerAsync(Guid id, CancellationToken ct = default);

    // ── D&D 5e Reference ─────────────────────────────────────────────────────

    [Get("/api/v1/dnd5e/spells")]
    Task<SpellPagedResult> GetDnd5eSpellsAsync(
        [Query] string? search = null, [Query] string? school = null,
        [Query] int? level = null, [Query] string? @class = null,
        [Query] int page = 1, [Query] int pageSize = 30,
        CancellationToken ct = default);

    [Get("/api/v1/dnd5e/spells/{id}")]
    Task<SpellDetailDto> GetDnd5eSpellAsync(Guid id, CancellationToken ct = default);

    [Get("/api/v1/dnd5e/monsters")]
    Task<MonsterPagedResult> GetDnd5eMonstersAsync(
        [Query] string? search = null, [Query] string? type = null,
        [Query] string? size = null, [Query] string? cr = null,
        [Query] int page = 1, [Query] int pageSize = 30,
        CancellationToken ct = default);

    [Get("/api/v1/dnd5e/monsters/{id}")]
    Task<MonsterDetailDto> GetDnd5eMonsterAsync(Guid id, CancellationToken ct = default);

    // ── Pathfinder 2e Reference ──────────────────────────────────────────────

    [Get("/api/v1/pf2e/spells")]
    Task<Pf2eSpellPagedResult> GetPf2eSpellsAsync(
        [Query] string? search = null, [Query] string? tradition = null,
        [Query] int? level = null, [Query] string? trait = null,
        [Query] int page = 1, [Query] int pageSize = 30,
        CancellationToken ct = default);

    [Get("/api/v1/pf2e/spells/{id}")]
    Task<Pf2eSpellDetailDto> GetPf2eSpellAsync(Guid id, CancellationToken ct = default);

    [Get("/api/v1/pf2e/monsters")]
    Task<Pf2eMonsterPagedResult> GetPf2eMonstersAsync(
        [Query] string? search = null, [Query] string? trait = null,
        [Query] string? size = null, [Query] int? level = null,
        [Query] int page = 1, [Query] int pageSize = 30,
        CancellationToken ct = default);

    [Get("/api/v1/pf2e/monsters/{id}")]
    Task<Pf2eMonsterDetailDto> GetPf2eMonsterAsync(Guid id, CancellationToken ct = default);

    [Get("/api/v1/pf2e/hazards")]
    Task<Pf2eHazardPagedResult> GetPf2eHazardsAsync(
        [Query] string? search = null, [Query] int? level = null,
        [Query] int page = 1, [Query] int pageSize = 30,
        CancellationToken ct = default);

    [Get("/api/v1/pf2e/hazards/{id}")]
    Task<Pf2eHazardDetailDto> GetPf2eHazardAsync(Guid id, CancellationToken ct = default);

    [Get("/api/v1/pf2e/vehicles")]
    Task<Pf2eVehiclePagedResult> GetPf2eVehiclesAsync(
        [Query] string? search = null, [Query] int? level = null,
        [Query] int page = 1, [Query] int pageSize = 30,
        CancellationToken ct = default);

    [Get("/api/v1/pf2e/vehicles/{id}")]
    Task<Pf2eVehicleDetailDto> GetPf2eVehicleAsync(Guid id, CancellationToken ct = default);

    // ── Users ─────────────────────────────────────────────────────────────────

    [Get("/api/v1/users/{id}")]
    Task<UserProfileDto> GetUserProfileAsync(Guid id, CancellationToken ct = default);

    [Put("/api/v1/users/me/profile")]
    Task UpdateProfileAsync([Body] UpdateProfileRequest request, CancellationToken ct = default);

    // Events
    [Get("/api/v1/events")]
    Task<EventsPagedResult> GetEventsAsync(
        [AliasAs("page")] int page, [AliasAs("pageSize")] int pageSize,
        [AliasAs("location")] string? location = null, [AliasAs("format")] string? format = null,
        CancellationToken ct = default);

    [Get("/api/v1/events/{id}")]
    Task<GameEventDetailDto> GetEventDetailAsync(Guid id, CancellationToken ct = default);

    [Post("/api/v1/events")]
    Task<Guid> CreateEventAsync([Body] CreateEventRequest request, CancellationToken ct = default);

    [Post("/api/v1/events/{id}/register")]
    Task RegisterForEventAsync(Guid id, CancellationToken ct = default);

    [Delete("/api/v1/events/{id}/register")]
    Task UnregisterFromEventAsync(Guid id, CancellationToken ct = default);

    [Patch("/api/v1/events/{id}/cancel")]
    Task CancelEventAsync(Guid id, CancellationToken ct = default);

    // Ratings
    [Get("/api/v1/ratings/{userId}")]
    Task<UserRatingsResult> GetUserRatingsAsync(Guid userId, CancellationToken ct = default);

    [Post("/api/v1/ratings/{userId}")]
    Task<Guid> RateUserAsync(Guid userId, [Body] RateUserRequest request, CancellationToken ct = default);

    [Delete("/api/v1/ratings/{ratingId}")]
    Task DeleteRatingAsync(Guid ratingId, CancellationToken ct = default);

    // Forum
    [Get("/api/v1/forum/categories")]
    Task<List<ForumCategoryDto>> GetForumCategoriesAsync(CancellationToken ct = default);

    [Get("/api/v1/forum/categories/{slug}/topics")]
    Task<ForumTopicsPagedResult> GetForumTopicsAsync(string slug, [AliasAs("page")] int page, [AliasAs("pageSize")] int pageSize, CancellationToken ct = default);

    [Get("/api/v1/forum/topics/{topicId}/posts")]
    Task<ForumTopicDetailResult> GetForumPostsAsync(Guid topicId, [AliasAs("page")] int page, [AliasAs("pageSize")] int pageSize, CancellationToken ct = default);

    [Post("/api/v1/forum/topics")]
    Task<Guid> CreateForumTopicAsync([Body] CreateForumTopicRequest request, CancellationToken ct = default);

    [Post("/api/v1/forum/topics/{topicId}/posts")]
    Task<Guid> CreateForumPostAsync(Guid topicId, [Body] CreateForumPostRequest request, CancellationToken ct = default);

    [Post("/api/v1/forum/posts/{postId}/like")]
    Task<ToggleLikeResult> ToggleForumPostLikeAsync(Guid postId, CancellationToken ct = default);

    // Homebrew
    [Get("/api/v1/homebrew")]
    Task<HomebrewPagedResult> SearchHomebrewAsync(
        [AliasAs("query")] string? query,
        [AliasAs("system")] string? system,
        [AliasAs("type")] HomebrewType? type,
        [AliasAs("tag")] string? tag,
        [AliasAs("page")] int page,
        [AliasAs("pageSize")] int pageSize,
        CancellationToken ct = default);

    [Get("/api/v1/homebrew/{id}")]
    Task<HomebrewDetailDto> GetHomebrewDetailAsync(Guid id, CancellationToken ct = default);

    [Post("/api/v1/homebrew")]
    Task<Guid> CreateHomebrewAsync([Body] CreateHomebrewRequest request, CancellationToken ct = default);

    [Delete("/api/v1/homebrew/{id}")]
    Task DeleteHomebrewAsync(Guid id, CancellationToken ct = default);

    [Post("/api/v1/homebrew/{id}/like")]
    Task<ToggleLikeResult> ToggleHomebrewLikeAsync(Guid id, CancellationToken ct = default);

    // Discussions
    [Get("/api/v1/discussions/{entityType}/{entitySlug}")]
    Task<List<DiscussionPostDto>> GetDiscussionAsync(string entityType, string entitySlug, CancellationToken ct = default);

    [Post("/api/v1/discussions/{entityType}/{entitySlug}")]
    Task<Guid> AddDiscussionPostAsync(string entityType, string entitySlug, [Body] AddDiscussionPostRequest request, CancellationToken ct = default);

    [Post("/api/v1/discussions/posts/{postId}/like")]
    Task<LikeResponse> ToggleDiscussionLikeAsync(Guid postId, CancellationToken ct = default);

    [Delete("/api/v1/discussions/posts/{postId}")]
    Task DeleteDiscussionPostAsync(Guid postId, CancellationToken ct = default);

    // ── Calendar ──────────────────────────────────────────────────────────────

    [Post("/api/calendar/preferences")]
    Task<CalendarPreferenceDto> UpsertCalendarPreferenceAsync([Body] UpsertCalendarPreferenceRequest request, CancellationToken ct = default);

    [Get("/api/calendar/preferences")]
    Task<CalendarPreferenceDto> GetCalendarPreferenceAsync(CancellationToken ct = default);

    [Get("/api/calendar/sessions/{id}.ics")]
    Task<string> GetSessionIcsAsync(Guid id, CancellationToken ct = default);

    [Get("/api/calendar/push/vapid-public-key")]
    Task<string> GetVapidPublicKeyAsync(CancellationToken ct = default);

    [Post("/api/calendar/push/subscribe")]
    Task SubscribePushAsync([Body] SubscribePushRequest request, CancellationToken ct = default);

    [Post("/api/calendar/push/unsubscribe")]
    Task UnsubscribePushAsync([Body] UnsubscribePushRequest request, CancellationToken ct = default);

    // ── Game Table ────────────────────────────────────────────────────────────

    [Get("/api/table/{sessionId}/state")]
    Task<TableStateDto> GetTableStateAsync(Guid sessionId, CancellationToken ct = default);

    [Post("/api/table/{sessionId}/messages")]
    Task<TableMessageDto> SendTableChatAsync(Guid sessionId, [Body] SendChatRequest request, CancellationToken ct = default);

    [Post("/api/table/{sessionId}/roll")]
    Task<TableMessageDto> RollTableDiceAsync(Guid sessionId, [Body] RollDiceRequest request, CancellationToken ct = default);

    [Put("/api/table/{sessionId}/showcase")]
    Task SetTableShowcaseAsync(Guid sessionId, [Body] SetShowcaseRequest request, CancellationToken ct = default);

    [Post("/api/table/{sessionId}/showcase/upload")]
    [Multipart]
    Task<AvatarUploadResponse> UploadTableShowcaseAsync(Guid sessionId, [AliasAs("file")] StreamPart file, CancellationToken ct = default);

    [Post("/api/table/{sessionId}/whisper")]
    Task<TableMessageDto> SendTableWhisperAsync(Guid sessionId, [Body] SendWhisperRequest request, CancellationToken ct = default);

    [Put("/api/table/{sessionId}/audio/track")]
    Task SetTableTrackAsync(Guid sessionId, [Body] SetTrackRequest request, CancellationToken ct = default);

    [Post("/api/table/{sessionId}/audio/upload")]
    [Multipart]
    Task<AvatarUploadResponse> UploadTableTrackAsync(Guid sessionId, [AliasAs("file")] StreamPart file, CancellationToken ct = default);

    [Post("/api/table/{sessionId}/audio/play")]
    Task PlayTableAudioAsync(Guid sessionId, [Body] AudioPositionRequest request, CancellationToken ct = default);

    [Post("/api/table/{sessionId}/audio/pause")]
    Task PauseTableAudioAsync(Guid sessionId, [Body] AudioPositionRequest request, CancellationToken ct = default);

    [Post("/api/table/{sessionId}/audio/seek")]
    Task SeekTableAudioAsync(Guid sessionId, [Body] AudioPositionRequest request, CancellationToken ct = default);

    [Delete("/api/table/{sessionId}/audio")]
    Task ClearTableAudioAsync(Guid sessionId, CancellationToken ct = default);

    [Post("/api/table/{sessionId}/tokens")]
    Task<TableTokenDto> AddTableTokenAsync(Guid sessionId, [Body] AddTokenRequest request, CancellationToken ct = default);

    [Put("/api/table/{sessionId}/tokens/{tokenId}/position")]
    Task MoveTableTokenAsync(Guid sessionId, Guid tokenId, [Body] TokenPositionRequest request, CancellationToken ct = default);

    [Delete("/api/table/{sessionId}/tokens/{tokenId}")]
    Task RemoveTableTokenAsync(Guid sessionId, Guid tokenId, CancellationToken ct = default);

    [Patch("/api/table/{sessionId}/tokens/{tokenId}/stats")]
    Task UpdateTableTokenStatsAsync(Guid sessionId, Guid tokenId, [Body] UpdateTokenStatsRequest request, CancellationToken ct = default);

    [Put("/api/table/{sessionId}/tokens/{tokenId}/visibility")]
    Task SetTableTokenVisibilityAsync(Guid sessionId, Guid tokenId, [Body] SetTokenVisibilityRequest request, CancellationToken ct = default);

    [Post("/api/table/{sessionId}/tokens/{tokenId}/image")]
    [Multipart]
    Task<AvatarUploadResponse> UploadTokenImageAsync(Guid sessionId, Guid tokenId, [AliasAs("file")] StreamPart file, CancellationToken ct = default);

    [Put("/api/table/{sessionId}/grid")]
    Task SetTableGridCellSizeAsync(Guid sessionId, [Body] SetGridCellSizeRequest request, CancellationToken ct = default);

    [Put("/api/table/{sessionId}/fog")]
    Task SetTableFogSettingsAsync(Guid sessionId, [Body] SetFogSettingsRequest request, CancellationToken ct = default);

    [Put("/api/table/{sessionId}/environment")]
    Task SetTableSceneEnvironmentAsync(Guid sessionId, [Body] SetSceneEnvironmentRequest request, CancellationToken ct = default);

    [Put("/api/table/{sessionId}/variant-rules")]
    Task SetTableVariantRulesAsync(Guid sessionId, [Body] SetVariantRulesRequest request, CancellationToken ct = default);

    [Put("/api/table/{sessionId}/encounter-table")]
    Task SetTableEncounterTableAsync(Guid sessionId, [Body] SetEncounterTableRequest request, CancellationToken ct = default);

    [Post("/api/table/{sessionId}/encounter-table/roll")]
    Task<TableMessageDto> RollTableEncounterTableAsync(Guid sessionId, CancellationToken ct = default);

    [Put("/api/table/{sessionId}/walls")]
    Task SetTableWallsAsync(Guid sessionId, [Body] SetWallsRequest request, CancellationToken ct = default);

    [Put("/api/table/{sessionId}/lights")]
    Task SetTableLightsAsync(Guid sessionId, [Body] SetLightsRequest request, CancellationToken ct = default);

    [Post("/api/table/{sessionId}/combat/start")]
    Task StartTableCombatAsync(Guid sessionId, CancellationToken ct = default);

    [Post("/api/table/{sessionId}/combat/end")]
    Task EndTableCombatAsync(Guid sessionId, CancellationToken ct = default);

    [Post("/api/table/{sessionId}/combat/next")]
    Task NextTableTurnAsync(Guid sessionId, CancellationToken ct = default);

    [Post("/api/table/{sessionId}/combat/previous")]
    Task PreviousTableTurnAsync(Guid sessionId, CancellationToken ct = default);

    [Post("/api/table/{sessionId}/scenes")]
    Task<CreateSceneResponse> CreateSceneAsync(Guid sessionId, [Body] CreateSceneRequest request, CancellationToken ct = default);

    [Put("/api/table/{sessionId}/scenes/{sceneId}")]
    Task RenameSceneAsync(Guid sessionId, Guid sceneId, [Body] CreateSceneRequest request, CancellationToken ct = default);

    [Delete("/api/table/{sessionId}/scenes/{sceneId}")]
    Task DeleteSceneAsync(Guid sessionId, Guid sceneId, CancellationToken ct = default);

    [Post("/api/table/{sessionId}/scenes/{sceneId}/activate")]
    Task ActivateSceneAsync(Guid sessionId, Guid sceneId, CancellationToken ct = default);

    [Get("/api/table/{sessionId}/characters")]
    Task<List<SessionCharacterDto>> GetSessionCharactersAsync(Guid sessionId, CancellationToken ct = default);

    [Post("/api/table/{sessionId}/tokens/{tokenId}/conditions")]
    Task ApplyTokenConditionAsync(Guid sessionId, Guid tokenId, [Body] ApplyConditionRequest request, CancellationToken ct = default);

    [Delete("/api/table/{sessionId}/tokens/{tokenId}/conditions/{slug}")]
    Task RemoveTokenConditionAsync(Guid sessionId, Guid tokenId, string slug, CancellationToken ct = default);

    [Get("/api/table/{sessionId}/journal")]
    Task<List<JournalEntryDto>> GetJournalEntriesAsync(Guid sessionId, CancellationToken ct = default);

    [Post("/api/table/{sessionId}/journal")]
    Task<JournalEntryDto> CreateJournalEntryAsync(Guid sessionId, [Body] CreateJournalEntryRequest request, CancellationToken ct = default);

    [Put("/api/table/{sessionId}/journal/{entryId}")]
    Task UpdateJournalEntryAsync(Guid sessionId, Guid entryId, [Body] CreateJournalEntryRequest request, CancellationToken ct = default);

    [Put("/api/table/{sessionId}/journal/{entryId}/published")]
    Task SetJournalEntryPublishedAsync(Guid sessionId, Guid entryId, [Body] SetJournalEntryPublishedRequest request, CancellationToken ct = default);

    [Put("/api/table/{sessionId}/journal/{entryId}/visibility")]
    Task SetJournalEntryVisibilityAsync(Guid sessionId, Guid entryId, [Body] SetJournalEntryVisibilityRequest request, CancellationToken ct = default);

    [Delete("/api/table/{sessionId}/journal/{entryId}")]
    Task DeleteJournalEntryAsync(Guid sessionId, Guid entryId, CancellationToken ct = default);

    [Post("/api/table/{sessionId}/journal/import-pdf")]
    [Multipart]
    Task<ImportAdventurePdfResponse> ImportAdventurePdfAsync(Guid sessionId, [AliasAs("file")] StreamPart file, CancellationToken ct = default);

    // ── Rules Reference 2.0 (системно-независимый справочник) ──────────────────

    [Get("/api/v1/rules/systems")]
    Task<List<GameSystemDto>> GetGameSystemsAsync(CancellationToken ct = default);

    [Get("/api/v1/rules/{systemSlug}/{category}")]
    Task<RuleEntryPageDto> GetRuleEntriesAsync(
        string systemSlug, string category, [Query] string? search = null,
        [Query] int page = 1, [Query] int pageSize = 40, CancellationToken ct = default);

    [Get("/api/v1/rules/{systemSlug}/{category}/{slug}")]
    Task<RuleEntryDetailDto> GetRuleEntryDetailAsync(
        string systemSlug, string category, string slug, CancellationToken ct = default);

    [Post("/api/v1/rules/{systemSlug}/{category}/batch")]
    Task<List<RuleEntryStatsDto>> GetRuleEntriesBySlugsAsync(
        string systemSlug, string category, [Body] BatchSlugsRequest request, CancellationToken ct = default);

    [Post("/api/v1/rules/{systemSlug}/multiclass")]
    Task<MulticlassResultDto> CalculateMulticlassAsync(
        string systemSlug, [Body] List<ClassLevelInputDto> classes, CancellationToken ct = default);

    [Post("/api/v1/rules/systems")]
    Task<CreateGameSystemResponse> CreateGameSystemAsync([Body] CreateGameSystemRequest request, CancellationToken ct = default);

    [Post("/api/v1/rules/{systemSlug}/{category}")]
    Task<CreateRuleEntryResponse> CreateRuleEntryAsync(
        string systemSlug, string category, [Body] CreateRuleEntryRequest request, CancellationToken ct = default);

    [Put("/api/v1/rules/{systemSlug}/{category}/{slug}")]
    Task UpdateRuleEntryAsync(
        string systemSlug, string category, string slug, [Body] CreateRuleEntryRequest request, CancellationToken ct = default);

    [Delete("/api/v1/rules/{systemSlug}/{category}/{slug}")]
    Task DeleteRuleEntryAsync(string systemSlug, string category, string slug, CancellationToken ct = default);

    // ── Тикеты поддержки ─────────────────────────────────────────────────────────

    [Post("/api/tickets")]
    [Multipart]
    Task<CreateTicketResponse> CreateTicketAsync(
        [AliasAs("title")] string title,
        [AliasAs("description")] string description,
        [AliasAs("contactInfo")] string? contactInfo,
        [AliasAs("files")] List<StreamPart>? files,
        CancellationToken ct = default);

    [Get("/api/tickets/me")]
    Task<PagedTicketsResult> GetMyTicketsAsync([Query] int page = 1, [Query] int pageSize = 20, CancellationToken ct = default);

    [Get("/api/tickets")]
    Task<List<TicketDto>> GetAllTicketsAsync(CancellationToken ct = default);

    [Patch("/api/tickets/{id}/status")]
    Task ChangeTicketStatusAsync(Guid id, [Body] ChangeTicketStatusRequest request, CancellationToken ct = default);

    [Get("/api/tickets/{id}")]
    Task<TicketDto> GetTicketByIdAsync(Guid id, CancellationToken ct = default);

    [Get("/api/tickets/{id}/comments")]
    Task<List<TicketCommentDto>> GetTicketCommentsAsync(Guid id, CancellationToken ct = default);

    [Post("/api/tickets/{id}/comments")]
    Task<Guid> AddTicketCommentAsync(Guid id, [Body] AddTicketCommentRequest request, CancellationToken ct = default);

    // ── Модерация ─────────────────────────────────────────────────────────────────

    [Get("/api/v1/users/admin")]
    Task<AdminUserPageDto> GetAllUsersAsync([Query] string? search = null, [Query] int page = 1, [Query] int pageSize = 30, CancellationToken ct = default);

    [Patch("/api/v1/users/admin/{id}/role")]
    Task ChangeUserRoleAsync(Guid id, [Body] ChangeRoleRequest request, CancellationToken ct = default);

    [Post("/api/v1/reports")]
    Task<Guid> CreateReportAsync([Body] CreateReportRequest request, CancellationToken ct = default);

    [Get("/api/v1/reports")]
    Task<List<ContentReportDto>> GetOpenReportsAsync(CancellationToken ct = default);

    [Patch("/api/v1/reports/{id}/resolve")]
    Task ResolveReportAsync(Guid id, [Body] ResolveReportRequest request, CancellationToken ct = default);

    [Get("/api/v1/moderation-log")]
    Task<List<ModerationLogEntryDto>> GetModerationLogAsync(CancellationToken ct = default);

    [Delete("/api/v1/forum/topics/{topicId}")]
    Task DeleteForumTopicAsync(Guid topicId, CancellationToken ct = default);

    [Delete("/api/v1/forum/posts/{postId}")]
    Task DeleteForumPostAsync(Guid postId, CancellationToken ct = default);

    [Put("/api/v1/forum/topics/{topicId}/pin")]
    Task SetForumTopicPinnedAsync(Guid topicId, [Body] SetPinnedRequest request, CancellationToken ct = default);

    [Put("/api/v1/forum/topics/{topicId}/lock")]
    Task SetForumTopicLockedAsync(Guid topicId, [Body] SetLockedRequest request, CancellationToken ct = default);

    // K.7 — макросы: личная библиотека, не привязана к сессии (см. Macro.cs).
    [Get("/api/v1/macros")]
    Task<List<MacroDto>> GetMyMacrosAsync(CancellationToken ct = default);

    [Post("/api/v1/macros")]
    Task<MacroDto> CreateMacroAsync([Body] CreateMacroRequest request, CancellationToken ct = default);

    [Put("/api/v1/macros/{id}")]
    Task UpdateMacroAsync(Guid id, [Body] UpdateMacroRequest request, CancellationToken ct = default);

    [Delete("/api/v1/macros/{id}")]
    Task DeleteMacroAsync(Guid id, CancellationToken ct = default);

    [Put("/api/v1/macros/{id}/hotbar-slot")]
    Task SetMacroHotbarSlotAsync(Guid id, [Body] SetHotbarSlotRequest request, CancellationToken ct = default);

    [Multipart]
    [Post("/api/v1/macros/import/foundry")]
    Task<List<MacroDto>> ImportFoundryMacrosAsync([AliasAs("file")] StreamPart file, CancellationToken ct = default);

    // J.9 — модули: экспорт своих макросов + своей системы справочника в один JSON-файл,
    // импорт такого же файла другим пользователем (см. ModuleManifest на бэкенде).
    [Post("/api/v1/modules/export")]
    Task<string> ExportModuleAsync([Body] ExportModuleRequest request, CancellationToken ct = default);

    [Multipart]
    [Post("/api/v1/modules/import")]
    Task<ImportModuleResponse> ImportModuleAsync([AliasAs("file")] StreamPart file, CancellationToken ct = default);
}

public sealed record MacroDto(
    Guid Id, string Name, string? ImageUrl, string Type, string Command,
    int HotbarSlot, DateTime CreatedAt, DateTime UpdatedAt);
public sealed record CreateMacroRequest(string Name, string? ImageUrl, string Type, string Command);
public sealed record UpdateMacroRequest(string Name, string? ImageUrl, string Type, string Command);
public sealed record SetHotbarSlotRequest(int Slot);

public sealed record ExportModuleRequest(
    string Name, string? Description, string? Version, List<Guid> MacroIds, string? SystemSlug);
public sealed record ImportModuleResponse(int MacrosImported, int RuleEntriesImported, string? SystemSlug);

public sealed record AvatarUploadResponse(string Url);

// ── Sessions ──────────────────────────────────────────────────────────────────

public enum SessionStatus { Planned, InProgress, Completed, Cancelled }
public enum ParticipantRole { Player, DungeonMaster }
public enum SessionFormat { Online, Offline, Hybrid }

public sealed record CreateSessionRequest(
    string Title, string? Description, string System, int MaxPlayers, DateTime ScheduledAt,
    SessionFormat Format, string? Location);

public sealed record CreateSessionResponse(Guid SessionId, string Title);
public sealed record ImportSessionResponse(Guid SessionId, string Title);
public sealed record ImportSessionRequest(
    string Title, string System, DateTime ScheduledAt, int MaxPlayers, string? Description = null);

public sealed record SessionSummaryDto(
    Guid Id, string Title, string? Description, string System,
    int MaxPlayers, int CurrentPlayers, DateTime ScheduledAt,
    SessionFormat Format, string? Location,
    SessionStatus Status, Guid OrganizerId, string OrganizerName);

public sealed record SessionDetailDto(
    Guid Id, string Title, string? Description, string System,
    int MaxPlayers, DateTime ScheduledAt, SessionFormat Format, string? Location, SessionStatus Status,
    Guid OrganizerId, string OrganizerName,
    List<SessionParticipantDto> Participants,
    bool IsCurrentUserParticipant, bool IsCurrentUserOrganizer);

public sealed record SessionParticipantDto(Guid UserId, string Username, ParticipantRole Role, DateTime JoinedAt);

public sealed record SessionReviewDto(
    Guid Id, Guid ReviewerId, string ReviewerUsername, Guid RevieweeId, string RevieweeUsername,
    int Score, string? Comment, DateTime CreatedAt);

public sealed record RateSessionParticipantRequest(Guid RevieweeId, int Score, string? Comment);

public sealed record UserSessionReviewDto(
    Guid Id, Guid SessionId, string SessionTitle, Guid ReviewerId, string ReviewerUsername,
    int Score, string? Comment, DateTime CreatedAt);

public sealed record UserSessionReviewsResult(
    List<UserSessionReviewDto> Reviews, double AverageScore, int TotalCount);

public sealed record UpdateSessionRequest(
    Guid SessionId, string Title, string? Description, string System, int MaxPlayers, DateTime ScheduledAt,
    SessionFormat Format, string? Location);

public sealed record ChangeStatusRequest(SessionStatus Status);

// ── Campaigns ─────────────────────────────────────────────────────────────────

public enum CampaignStatus { Active, Paused, Completed, Archived }
public enum CampaignRole { DungeonMaster, Player }

public sealed record CreateCampaignRequest(string Title, string? Description, string System);
public sealed record CreateCampaignResponse(Guid CampaignId, string Title);
public sealed record UpdateCampaignRequest(string Title, string? Description, string System);
public sealed record AddParticipantRequest(Guid UserId);
public sealed record ChangeCampaignStatusRequest(CampaignStatus Status);

public sealed record ImportCampaignResponse(Guid CampaignId, string Title);
public sealed record ImportCampaignRequest(string Title, string System, string? Description = null);

public sealed record CampaignSummaryDto(
    Guid Id, string Title, string? Description, string System,
    CampaignStatus Status, int ParticipantCount, bool IsOrganizer,
    DateTime CreatedAt, DateTime UpdatedAt);

public sealed record CampaignDetailDto(
    Guid Id, string Title, string? Description, string System,
    CampaignStatus Status, Guid OrganizerId, string OrganizerName,
    List<CampaignParticipantDto> Participants,
    bool IsCurrentUserOrganizer, bool IsCurrentUserParticipant,
    DateTime CreatedAt, DateTime UpdatedAt);

public sealed record CampaignParticipantDto(
    Guid UserId, string Username, CampaignRole Role, DateTime JoinedAt);

// ── Session Notes ─────────────────────────────────────────────────────────────

public sealed record CreateNoteRequest(Guid CampaignId, string Title, string Content, DateTime SessionDate);
public sealed record CreateNoteResponse(Guid NoteId);
public sealed record UpdateNoteRequest(string Title, string Content, DateTime SessionDate);

public sealed record SessionNoteSummaryDto(
    Guid Id, Guid CampaignId, Guid AuthorId, string AuthorName,
    string Title, DateTime SessionDate, DateTime UpdatedAt);

public sealed record SessionNoteDetailDto(
    Guid Id, Guid CampaignId, Guid AuthorId, string AuthorName,
    string Title, string Content, DateTime SessionDate,
    DateTime CreatedAt, DateTime UpdatedAt, bool IsAuthor);

// ── Encounters ────────────────────────────────────────────────────────────────

public enum EncounterDifficulty { Trivial, Easy, Medium, Hard, Deadly }

public sealed record EncounterEntryInput(string Name, int Count, string? Notes);

public sealed record CreateEncounterRequest(
    Guid CampaignId, string Title, string? Description,
    EncounterDifficulty Difficulty, string? Notes,
    List<EncounterEntryInput> Entries);

public sealed record CreateEncounterResponse(Guid EncounterId);

public sealed record UpdateEncounterRequest(
    string Title, string? Description,
    EncounterDifficulty Difficulty, string? Notes,
    List<EncounterEntryInput> Entries);

public sealed record EncounterSummaryDto(
    Guid Id, Guid CampaignId, string Title, string? Description,
    EncounterDifficulty Difficulty, int EntryCount, DateTime UpdatedAt);

public sealed record EncounterDetailDto(
    Guid Id, Guid CampaignId, Guid CreatedById,
    string Title, string? Description,
    EncounterDifficulty Difficulty, string? Notes,
    List<EncounterEntryDetail> Entries,
    bool IsCreator, DateTime CreatedAt, DateTime UpdatedAt);

public sealed record EncounterEntryDetail(string Name, int Count, string? Notes);

// ── Initiative Tracker ────────────────────────────────────────────────────────

public enum EntryStatus { Active, Unconscious, Dead }

public sealed record CreateTrackerRequest(Guid CampaignId, string Name);
public sealed record CreateTrackerResponse(Guid TrackerId);
public sealed record TrackerEntryInput(string Name, int Initiative, int MaxHp, int CurrentHp,
    int ArmorClass, bool IsPlayerCharacter, string? Notes);
public sealed record UpdateEntryRequest(int CurrentHp, EntryStatus Status, string? Notes);
public sealed record SyncFromTableRequest(Guid SessionId);
public sealed record LinkSessionRequest(Guid? SessionId);

public sealed record TrackerSummaryDto(
    Guid Id, Guid CampaignId, string Name, int Round, bool IsActive,
    int EntryCount, DateTime UpdatedAt);

public sealed record TrackerDetailDto(
    Guid Id, Guid CampaignId, Guid OwnerId,
    string Name, int Round, int ActiveEntryIndex, bool IsActive,
    Guid? LinkedSessionId,
    List<TrackerEntryDto> Entries,
    bool IsOwner, DateTime UpdatedAt);

public sealed record TrackerConditionSnapshot(string Slug, string Name, int? Value);

public sealed record TrackerEntryDto(
    Guid Id, string Name, int Initiative, int MaxHp, int CurrentHp,
    int ArmorClass, EntryStatus Status, bool IsPlayerCharacter, string? Notes, int SortOrder,
    Guid? LinkedTokenId, List<TrackerConditionSnapshot> Conditions);

// ── D&D 5e DTOs ───────────────────────────────────────────────────────────────

public sealed record SpellSummaryDto(
    Guid Id, string Name, int Level, string School,
    string CastingTime, string Range, string Duration,
    bool Concentration, bool Ritual, string Classes);

public sealed record SpellDetailDto(
    Guid Id, string Slug, string Name, int Level, string School,
    string CastingTime, string Range, string Components, string? Material,
    string Duration, bool Concentration, bool Ritual,
    string Description, string? HigherLevel, string Classes, string Source);

public sealed record SpellPagedResult(
    List<SpellSummaryDto> Items, int Total, int Page, int PageSize, int TotalPages);

public sealed record MonsterSummaryDto(
    Guid Id, string Name, string Size, string Type, string? Subtype,
    string Alignment, int ArmorClass, int HitPoints,
    string ChallengeRating, int Xp);

public sealed record MonsterDetailDto(
    Guid Id, string Slug, string Name, string Size, string Type, string? Subtype,
    string Alignment, int ArmorClass, string? ArmorDesc, int HitPoints, string HitDice,
    string Speed,
    int Strength, int Dexterity, int Constitution,
    int Intelligence, int Wisdom, int Charisma,
    string ChallengeRating, int Xp,
    string? Senses, string? Languages,
    string? Actions, string? SpecialAbilities, string? Reactions, string? LegendaryActions,
    string Source);

public sealed record MonsterPagedResult(
    List<MonsterSummaryDto> Items, int Total, int Page, int PageSize, int TotalPages);

// ── Pathfinder 2e ────────────────────────────────────────────────────────────

public sealed record Pf2eSpellSummaryDto(
    Guid Id, string Slug, string Name, int Level, string Traditions, string Traits,
    string Cast, string? Range, string Duration);

public sealed record Pf2eSpellDetailDto(
    Guid Id, string Slug, string Name, int Level, string Traditions, string Traits,
    string Cast, string? Range, string? Area, string? Targets, string Duration,
    string Description, string? Heightened, string Source,
    string? DamageJson, string? HeighteningJson, string? DefenseJson);

public sealed record Pf2eSpellPagedResult(
    List<Pf2eSpellSummaryDto> Items, int Total, int Page, int PageSize, int TotalPages);

public sealed record Pf2eMonsterSummaryDto(
    Guid Id, string Slug, string Name, int Level, string Size, string Traits,
    int ArmorClass, int HitPoints);

public sealed record Pf2eMonsterDetailDto(
    Guid Id, string Slug, string Name, int Level, string Size, string Traits,
    int Perception, string? Senses, string? Languages, string? Skills,
    int Strength, int Dexterity, int Constitution,
    int Intelligence, int Wisdom, int Charisma,
    int ArmorClass, int Fortitude, int Reflex, int Will, int HitPoints,
    string Speed, string? Attacks, string? Abilities, string Source, string? AttacksJson,
    string? ResistancesJson, string? WeaknessesJson, string? ImmunitiesJson, string? AurasJson,
    string? ModifiersJson);

public sealed record Pf2eMonsterPagedResult(
    List<Pf2eMonsterSummaryDto> Items, int Total, int Page, int PageSize, int TotalPages);

public sealed record Pf2eHazardSummaryDto(
    Guid Id, string Slug, string Name, string NameRu, int Level, string Traits, int StealthDc);

public sealed record Pf2eHazardDetailDto(
    Guid Id, string Slug, string Name, string NameRu, int Level, string Traits,
    int StealthDc, string? StealthNote, string? Description, string? DisableText,
    int? ArmorClass, int? Fortitude, int? Reflex, int? Hardness, int? HitPoints,
    string? Immunities, string? AbilitiesText, string? ResetText, string Source,
    string? AttacksJson);

public sealed record Pf2eHazardPagedResult(
    List<Pf2eHazardSummaryDto> Items, int Total, int Page, int PageSize, int TotalPages);

public sealed record Pf2eVehicleSummaryDto(
    Guid Id, string Slug, string Name, string NameRu, int Level, string? Size, int? ArmorClass, int? HitPoints);

public sealed record Pf2eVehicleDetailDto(
    Guid Id, string Slug, string Name, string NameRu, int Level, string? Size, string? Price,
    string? Dimensions, string? Crew, string? Passengers, string? PilotingCheck,
    int? ArmorClass, int? Fortitude, int? Hardness, int? HitPoints, int? BrokenThreshold,
    string? Immunities, string? Speed, string? Collision, string? AbilitiesText, string Source);

public sealed record Pf2eVehiclePagedResult(
    List<Pf2eVehicleSummaryDto> Items, int Total, int Page, int PageSize, int TotalPages);

// ── Users ─────────────────────────────────────────────────────────────────────

public sealed record UpdateProfileRequest(string? DisplayName, string? Bio, string? City);

public sealed record UserProfileDto(
    Guid Id,
    string Username,
    string? DisplayName,
    string? Bio,
    string? City,
    string? AvatarUrl,
    string ExperienceLevel,
    DateTime MemberSince,
    List<PublicCharacterDto> Characters,
    List<PublicCampaignDto> Campaigns);

public sealed record PublicCharacterDto(Guid Id, string Name, string Race, string Class, int Level);
public sealed record PublicCampaignDto(Guid Id, string Title, string System, string Status);

// ── Forum ─────────────────────────────────────────────────────────────────────

public sealed record ForumCategoryDto(Guid Id, string Name, string Description, string Slug, int DisplayOrder, int TopicCount);

public sealed record ForumTopicsPagedResult(List<ForumTopicDto> Items, int Total, int Page, int PageSize)
{
    public int TotalPages => (int)Math.Ceiling((double)Total / PageSize);
}

public sealed record ForumTopicDto(
    Guid Id, string Title, Guid AuthorId, string AuthorUsername,
    bool IsPinned, bool IsLocked, DateTime CreatedAt, DateTime? LastPostAt, int PostCount);

public sealed record ForumTopicDetailResult(
    Guid Id, string Title, bool IsPinned, bool IsLocked,
    string CategorySlug, string CategoryName, ForumPostsPagedResult Posts);

public sealed record ForumPostsPagedResult(List<ForumPostDto> Items, int Total, int Page, int PageSize)
{
    public int TotalPages => (int)Math.Ceiling((double)Total / PageSize);
}

public sealed record ForumPostDto(
    Guid Id, Guid AuthorId, string AuthorUsername, string? AuthorAvatarUrl,
    string Content, DateTime CreatedAt, DateTime? UpdatedAt, int LikeCount, bool LikedByMe);

public sealed record CreateForumTopicRequest(Guid CategoryId, string Title, string FirstPostContent);
public sealed record CreateForumPostRequest(string Content);
public sealed record ToggleLikeResult(bool Liked, int LikeCount);

// ── Homebrew ──────────────────────────────────────────────────────────────────

public enum HomebrewType { Spell, Monster, Class, Subclass, Race, Subrace, Item, Background, Feat, Other }

public sealed record HomebrewPagedResult(List<HomebrewItemDto> Items, int Total, int Page, int PageSize)
{
    public int TotalPages => (int)Math.Ceiling((double)Total / PageSize);
}

public sealed record HomebrewItemDto(
    Guid Id, string Title, string Description, string System, string Type,
    string Tags, Guid AuthorId, string AuthorUsername, int LikeCount, bool LikedByMe, DateTime CreatedAt);

public sealed record HomebrewDetailDto(
    Guid Id, string Title, string Description, string System, string Type,
    string Content, string Tags, Guid AuthorId, string AuthorUsername,
    int LikeCount, bool LikedByMe, DateTime CreatedAt, DateTime? UpdatedAt);

public sealed record CreateHomebrewRequest(
    string Title, string Description, string System, HomebrewType Type, string Content, string Tags);

// ── Events ────────────────────────────────────────────────────────────────────

public sealed record GameEventSummaryDto(
    Guid Id, string Title, string System, string Format,
    string? Location, string? OnlineLink, DateTime StartsAt,
    int MaxParticipants, int ParticipantCount,
    Guid OrganizerId, string OrganizerUsername, bool IsCancelled);

public sealed record EventsPagedResult(List<GameEventSummaryDto> Items, int Total, int Page, int PageSize)
{
    public int TotalPages => (int)Math.Ceiling((double)Total / PageSize);
}

public sealed record EventParticipantDto(Guid UserId, string Username, string? AvatarUrl, DateTime RegisteredAt);

public sealed record GameEventDetailDto(
    Guid Id, string Title, string? Description, string System,
    string Format, string? Location, string? OnlineLink,
    DateTime StartsAt, int MaxParticipants, bool IsCancelled,
    Guid OrganizerId, string OrganizerUsername, string? OrganizerAvatarUrl,
    DateTime CreatedAt, List<EventParticipantDto> Participants);

public sealed record CreateEventRequest(
    string Title, string? Description, string System, string Format,
    string? Location, string? OnlineLink, DateTime StartsAt, int MaxParticipants);

// ── Ratings ───────────────────────────────────────────────────────────────────

public sealed record UserRatingDto(
    Guid Id, Guid RaterId, string RaterUsername, string? RaterAvatarUrl,
    int Score, string? Comment, string Role, DateTime CreatedAt);

public sealed record UserRatingsResult(List<UserRatingDto> Ratings, double AverageScore, int TotalCount);

public sealed record RateUserRequest(int Score, string? Comment, string Role);

// ── Discussions ───────────────────────────────────────────────────────────────

public sealed record DiscussionPostDto(
    Guid Id, Guid AuthorId, string AuthorUsername, string? AuthorAvatarUrl,
    string Content, Guid? ParentId, int LikeCount, bool IsLikedByMe, bool IsOwn,
    DateTime CreatedAt, List<DiscussionPostDto> Replies);

public sealed record AddDiscussionPostRequest(string Content, Guid? ParentId = null);

public sealed record LikeResponse(bool IsLiked);

// ── Calendar ──────────────────────────────────────────────────────────────────

public sealed record CalendarPreferenceDto(Guid CalendarToken, int ReminderMinutes, bool PushEnabled);
public sealed record UpsertCalendarPreferenceRequest(int ReminderMinutes, bool RegenerateToken = false);

// ── Game Table ────────────────────────────────────────────────────────────────

public enum TableMessageKind { Chat, Roll, System, Whisper }

public sealed record TableMessageDto(
    Guid Id, Guid SenderId, string SenderUsername,
    Guid? RecipientId, string? RecipientUsername,
    TableMessageKind Kind, string Content, DateTime CreatedAt);

public sealed record TableParticipantDto(Guid UserId, string Username, string? AvatarUrl, bool IsDungeonMaster);

public sealed record AudioStateDto(
    string? TrackUrl, string? TrackTitle,
    bool IsPlaying, double PositionSeconds, DateTime ServerTimestamp);

public sealed record TableTokenDto(
    Guid Id, string Label, string? ImageUrl, string Color,
    double X, double Y, int Width, int Height, int Rotation, Guid? OwnerId, bool CanMove,
    string CombatantType, Guid? CombatantId, int? CurrentHp, int? MaxHp, int? ArmorClass,
    List<TokenConditionDto> Conditions, int? Initiative, bool HasDarkvision, bool HasLowLightVision,
    List<Guid>? VisibleToUserIds,
    int? CurrentStamina = null, int? MaxStamina = null,
    List<Guid>? CoOwnerIds = null);

public sealed record TokenConditionDto(Guid Id, string Slug, string Name, int? Value);
public sealed record ApplyConditionRequest(string Slug, string Name, int? Value);

public sealed record SceneSummaryDto(Guid Id, string Name);

public sealed record TableStateDto(
    Guid SessionId, string Title, string? ShowcaseImageUrl, int GridCellSizePx,
    bool IsOrganizer, bool CanAccess,
    List<TableParticipantDto> Participants,
    List<TableMessageDto> RecentMessages,
    AudioStateDto Audio,
    List<TableTokenDto> Tokens,
    bool FogEnabled, int VisionRadiusFeet, string? WallsJson,
    bool CombatActive, int CombatRound, Guid? CombatTurnTokenId,
    string? LightsJson,
    string? TerrainTagsJson, string AmbientLighting,
    List<SceneSummaryDto> Scenes, Guid ActiveSceneId,
    bool ProficiencyWithoutLevel, bool AutomaticBonusProgression, bool FreeArchetype,
    bool GradualAbilityBoosts, bool StaminaVariant, string? EncounterTableJson);

public sealed record SendChatRequest(string Content);
public sealed record RollDiceRequest(string Expression, int? Dc = null, string? Label = null);
public sealed record SetShowcaseRequest(string? ImageUrl);
public sealed record SendWhisperRequest(Guid RecipientUserId, string Content);
public sealed record SetTrackRequest(string TrackUrl, string? TrackTitle);
public sealed record AudioPositionRequest(double PositionSeconds);
public sealed record AddTokenRequest(
    string Label, string? ImageUrl, string Color, double X, double Y, Guid? OwnerUserId,
    int Width = 1, int Height = 1, string CombatantType = "None", Guid? CombatantId = null);
public sealed record TokenPositionRequest(double X, double Y);
public sealed record UpdateTokenStatsRequest(
    int? CurrentHp, int? Width, int? Height, int? Rotation,
    bool SetInitiative = false, int? Initiative = null,
    bool? HasDarkvision = null,
    bool? HasLowLightVision = null,
    int? CurrentStamina = null,
    int? MaxStamina = null,
    Guid? AddCoOwnerId = null,
    Guid? RemoveCoOwnerId = null);
public sealed record SetTokenVisibilityRequest(List<Guid>? VisibleToUserIds);
public sealed record SetGridCellSizeRequest(int Px);
public sealed record SetFogSettingsRequest(bool Enabled, int VisionRadiusFeet);
public sealed record SetVariantRulesRequest(
    bool ProficiencyWithoutLevel, bool AutomaticBonusProgression, bool FreeArchetype,
    bool GradualAbilityBoosts, bool StaminaVariant);
public sealed record SetEncounterTableRequest(string? EncounterTableJson);
public sealed record SetSceneEnvironmentRequest(string? TerrainTagsJson, string AmbientLighting);
public sealed record SetWallsRequest(string? WallsJson);
public sealed record SetLightsRequest(string? LightsJson);
public sealed record CreateSceneRequest(string Name);
public sealed record CreateSceneResponse(Guid Id, string Name);
public sealed record JournalEntryDto(
    Guid Id, string Title, string ContentMarkdown, bool IsPublished,
    Guid? ParentId, Guid? CampaignId, List<Guid>? VisibleToUserIds,
    DateTime CreatedAt, DateTime UpdatedAt);
public sealed record CreateJournalEntryRequest(
    string Title, string ContentMarkdown, Guid? ParentId = null, Guid? CampaignId = null);
public sealed record SetJournalEntryVisibilityRequest(List<Guid>? VisibleToUserIds);
public sealed record SetJournalEntryPublishedRequest(bool Published);
public sealed record ImportAdventurePdfResponse(Guid FolderEntryId, int PagesImported, List<ImportedMapImageDto> Images);
public sealed record ImportedMapImageDto(int PageNumber, string Url, int Width, int Height);
public sealed record SessionCharacterDto(
    Guid Id, string Name, string? AvatarUrl, Guid OwnerId, string OwnerUsername,
    int CurrentHitPoints, int MaxHitPoints, int ArmorClass);
public sealed record SubscribePushRequest(string Endpoint, string P256dh, string Auth);
public sealed record UnsubscribePushRequest(string Endpoint);

// ── Rules Reference 2.0 ─────────────────────────────────────────────────────────

public sealed record GameSystemDto(Guid Id, string Slug, string Name, bool IsOfficial, bool IsMine);

public sealed record RuleEntrySummaryDto(Guid Id, string Slug, string Title, string? Summary, string[] Tags);

public sealed record RuleEntryPageDto(List<RuleEntrySummaryDto> Items, int Total, int Page, int PageSize)
{
    public int TotalPages => Math.Max(1, (int)Math.Ceiling((double)Total / PageSize));
}

public sealed record RuleEntryDetailDto(
    Guid Id, string SystemSlug, string Category, string Slug, string Title,
    string? Summary, string? ContentMarkdown, string StatsJson,
    string[] Tags, bool IsHomebrew, string Source, bool CanEdit);

public sealed record RuleEntryStatsDto(string Slug, string Title, string StatsJson);
public sealed record BatchSlugsRequest(List<string> Slugs);

public sealed record CreateGameSystemRequest(string Name);
public sealed record CreateGameSystemResponse(Guid Id, string Slug, string Name);

public sealed record CreateRuleEntryRequest(
    string Title, string? Summary, string? ContentMarkdown, string? StatsJson, string[]? Tags);
public sealed record CreateRuleEntryResponse(Guid Id, string Slug);

public sealed record ClassLevelInputDto(string ClassSlug, int Level);
public sealed record ClassLevelResultDto(string ClassTitle, int Level, string HitDice, int AverageHpContribution);
public sealed record MulticlassResultDto(
    int TotalLevel, int ProficiencyBonus, List<ClassLevelResultDto> Classes, List<string> HitDicePool);

// ── Тикеты поддержки ─────────────────────────────────────────────────────────

public sealed record CreateTicketResponse(Guid Id);

public sealed record TicketAttachmentDto(Guid Id, string Url, string FileName, string ContentType);

public sealed record TicketDto(
    Guid Id, string Title, string Description, string? ContactInfo,
    string Status, DateTime CreatedAt, DateTime UpdatedAt,
    List<TicketAttachmentDto> Attachments);

public sealed record PagedTicketsResult(List<TicketDto> Items, int Total, int Page, int PageSize)
{
    public int TotalPages => Math.Max(1, (int)Math.Ceiling((double)Total / PageSize));
}

public sealed record ChangeTicketStatusRequest(string Status);
public sealed record AddTicketCommentRequest(string Body);
public sealed record TicketCommentDto(Guid Id, Guid TicketId, Guid AuthorId, string AuthorUsername, string Body, DateTime CreatedAt);

// ── Модерация ─────────────────────────────────────────────────────────────────

public sealed record AdminUserDto(Guid Id, string Username, string Email, string Role, DateTime CreatedAt);

public sealed record AdminUserPageDto(List<AdminUserDto> Items, int Total, int Page, int PageSize)
{
    public int TotalPages => Math.Max(1, (int)Math.Ceiling((double)Total / PageSize));
}

public sealed record ChangeRoleRequest(string Role);

public sealed record CreateReportRequest(string EntityType, Guid EntityId, string Reason);
public sealed record SetPinnedRequest(bool Pinned);
public sealed record SetLockedRequest(bool Locked);

public sealed record ContentReportDto(
    Guid Id, string EntityType, Guid EntityId, string Reason,
    Guid ReporterId, string ReporterUsername, DateTime CreatedAt);

public sealed record ResolveReportRequest(string Status);

public sealed record ModerationLogEntryDto(
    Guid Id, Guid ActorUserId, string ActorUsername, string Action,
    string TargetType, Guid TargetId, DateTime CreatedAt, string? Details);
