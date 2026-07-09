using TTRPGHub.Entities.Pf2e;

namespace TTRPGHub.Repositories.Pf2e;

public interface IPf2eVehicleRepository
{
    Task<(IReadOnlyList<Pf2eVehicle> Items, int Total)> SearchAsync(
        string? search, int? level, int page, int pageSize, CancellationToken ct = default);

    Task<Pf2eVehicle?> GetByIdAsync(Pf2eVehicleId id, CancellationToken ct = default);
    Task<bool> AnyAsync(CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<Pf2eVehicle> vehicles, CancellationToken ct = default);
}
