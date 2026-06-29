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
public sealed record CreateCharacterResponse(Guid CharacterId, string Name, string Race, string Class, int Level);

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
    DateTime UpdatedAt
);

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

    [Get("/api/characters/{id}")]
    Task<CharacterDetailDto> GetCharacterByIdAsync(Guid id, CancellationToken ct = default);

    [Put("/api/characters/{id}")]
    Task UpdateCharacterAsync(Guid id, [Body] UpdateCharacterRequest request, CancellationToken ct = default);

    [Post("/api/characters/{id}/avatar")]
    [Multipart]
    Task<AvatarUploadResponse> UploadAvatarAsync(Guid id, [AliasAs("file")] StreamPart file, CancellationToken ct = default);

    [Get("/api/sessions/upcoming")]
    Task<List<SessionSummaryDto>> GetUpcomingSessionsAsync([Query] int page = 1, [Query] int pageSize = 20, CancellationToken ct = default);

    [Get("/api/sessions/me")]
    Task<List<SessionSummaryDto>> GetMySessionsAsync(CancellationToken ct = default);

    [Get("/api/sessions/{id}")]
    Task<SessionDetailDto> GetSessionDetailAsync(Guid id, CancellationToken ct = default);

    [Post("/api/sessions")]
    Task<CreateSessionResponse> CreateSessionAsync([Body] CreateSessionRequest request, CancellationToken ct = default);

    [Put("/api/sessions/{id}")]
    Task UpdateSessionAsync(Guid id, [Body] UpdateSessionRequest request, CancellationToken ct = default);

    [Post("/api/sessions/{id}/join")]
    Task JoinSessionAsync(Guid id, CancellationToken ct = default);

    [Post("/api/sessions/{id}/leave")]
    Task LeaveSessionAsync(Guid id, CancellationToken ct = default);

    [Patch("/api/sessions/{id}/status")]
    Task ChangeSessionStatusAsync(Guid id, [Body] ChangeStatusRequest request, CancellationToken ct = default);

    // ── Campaigns ────────────────────────────────────────────────────────────

    [Get("/api/v1/campaigns/me")]
    Task<List<CampaignSummaryDto>> GetMyCampaignsAsync(CancellationToken ct = default);

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

    // ── Users ─────────────────────────────────────────────────────────────────

    [Get("/api/v1/users/{id}")]
    Task<UserProfileDto> GetUserProfileAsync(Guid id, CancellationToken ct = default);
}

public sealed record AvatarUploadResponse(string Url);

// ── Sessions ──────────────────────────────────────────────────────────────────

public enum SessionStatus { Planned, InProgress, Completed, Cancelled }
public enum ParticipantRole { Player, DungeonMaster }

public sealed record CreateSessionRequest(
    string Title, string? Description, string System, int MaxPlayers, DateTime ScheduledAt);

public sealed record CreateSessionResponse(Guid SessionId, string Title);

public sealed record SessionSummaryDto(
    Guid Id, string Title, string? Description, string System,
    int MaxPlayers, int CurrentPlayers, DateTime ScheduledAt,
    SessionStatus Status, Guid OrganizerId, string OrganizerName);

public sealed record SessionDetailDto(
    Guid Id, string Title, string? Description, string System,
    int MaxPlayers, DateTime ScheduledAt, SessionStatus Status,
    Guid OrganizerId, string OrganizerName,
    List<SessionParticipantDto> Participants,
    bool IsCurrentUserParticipant, bool IsCurrentUserOrganizer);

public sealed record SessionParticipantDto(Guid UserId, string Username, ParticipantRole Role, DateTime JoinedAt);

public sealed record UpdateSessionRequest(
    Guid SessionId, string Title, string? Description, string System, int MaxPlayers, DateTime ScheduledAt);

public sealed record ChangeStatusRequest(SessionStatus Status);

// ── Campaigns ─────────────────────────────────────────────────────────────────

public enum CampaignStatus { Active, Paused, Completed, Archived }
public enum CampaignRole { DungeonMaster, Player }

public sealed record CreateCampaignRequest(string Title, string? Description, string System);
public sealed record CreateCampaignResponse(Guid CampaignId, string Title);
public sealed record UpdateCampaignRequest(string Title, string? Description, string System);
public sealed record AddParticipantRequest(Guid UserId);
public sealed record ChangeCampaignStatusRequest(CampaignStatus Status);

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

public sealed record TrackerSummaryDto(
    Guid Id, Guid CampaignId, string Name, int Round, bool IsActive,
    int EntryCount, DateTime UpdatedAt);

public sealed record TrackerDetailDto(
    Guid Id, Guid CampaignId, Guid OwnerId,
    string Name, int Round, int ActiveEntryIndex, bool IsActive,
    List<TrackerEntryDto> Entries,
    bool IsOwner, DateTime UpdatedAt);

public sealed record TrackerEntryDto(
    Guid Id, string Name, int Initiative, int MaxHp, int CurrentHp,
    int ArmorClass, EntryStatus Status, bool IsPlayerCharacter, string? Notes, int SortOrder);

// ── D&D 5e DTOs ───────────────────────────────────────────────────────────────

public sealed record SpellSummaryDto(
    Guid Id, string Name, int Level, string School,
    string CastingTime, string Range, string Duration,
    bool Concentration, bool Ritual, string Classes);

public sealed record SpellDetailDto(
    Guid Id, string Name, int Level, string School,
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
    Guid Id, string Name, string Size, string Type, string? Subtype,
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

// ── Users ─────────────────────────────────────────────────────────────────────

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
