namespace TTRPGHub.Entities.Pf2e;

public readonly record struct Pf2eVehicleId(Guid Value)
{
    public static Pf2eVehicleId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}
