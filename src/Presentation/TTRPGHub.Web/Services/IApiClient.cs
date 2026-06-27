using Refit;

namespace TTRPGHub.Web.Services;

// ── Auth ─────────────────────────────────────────────────────────────────────

public sealed record RegisterRequest(string Username, string Email, string Password);
public sealed record RegisterResponse(Guid UserId, string Username, string Email);

public sealed record LoginRequest(string Email, string Password);
public sealed record LoginResponse(string AccessToken, string RefreshToken, string Username);

// ── Characters ────────────────────────────────────────────────────────────────

public sealed record CreateCharacterRequest(string Name, string Race, string Class, int Level);
public sealed record CreateCharacterResponse(Guid CharacterId, string Name, string Race, string Class, int Level);

public sealed record CharacterSummaryDto(
    Guid Id, string Name, string Race, string Class,
    int Level, DateTime UpdatedAt);

public sealed record CharacterDto(
    Guid Id, Guid OwnerId, string Name, string Race, string Class,
    int Level, string? Notes, bool IsPublic, DateTime CreatedAt, DateTime UpdatedAt);

// ── Refit interface ───────────────────────────────────────────────────────────

public interface IApiClient
{
    [Post("/api/auth/register")]
    Task<RegisterResponse> RegisterAsync([Body] RegisterRequest request, CancellationToken ct = default);

    [Post("/api/auth/login")]
    Task<LoginResponse> LoginAsync([Body] LoginRequest request, CancellationToken ct = default);

    [Get("/api/characters/me")]
    Task<List<CharacterSummaryDto>> GetMyCharactersAsync(CancellationToken ct = default);

    [Post("/api/characters")]
    Task<CreateCharacterResponse> CreateCharacterAsync([Body] CreateCharacterRequest request, CancellationToken ct = default);

    [Get("/api/characters/{id}")]
    Task<CharacterDto> GetCharacterByIdAsync(Guid id, CancellationToken ct = default);
}
