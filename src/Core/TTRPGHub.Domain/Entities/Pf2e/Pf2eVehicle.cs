using TTRPGHub.Common;

namespace TTRPGHub.Entities.Pf2e;

// N.9 — транспорт (корабли, повозки, планеры и т.д.) как отдельный тип актёра со своими
// HP/AC/Твердостью и манёврами, отдельный от монстров/опасностей: у транспорта нет Восприятия/
// характеристик существа, зато есть Экипаж/Пассажиры/Проверка пилотирования/Столкновение —
// собственный набор полей, не влезающий ни в Pf2eMonster, ни в Pf2eHazard. Источник данных —
// pf2e-ru-translation (тот же, что и у N.1 hazards, Community Use Policy + OGL, см. /licenses) —
// единый файл game_mastering/subsystems/Vehicles.rst содержит статблоки транспорта в том же
// формате RST, что и hazards.rst для опасностей.
public sealed class Pf2eVehicle : Entity<Pf2eVehicleId>
{
    public new Pf2eVehicleId Id { get; private set; }
    public string Slug { get; private set; } = "";
    public string Name { get; private set; } = "";
    public string NameRu { get; private set; } = "";
    public int Level { get; private set; }
    public string? Size { get; private set; }
    public string? Price { get; private set; }
    public string? Dimensions { get; private set; }
    public string? Crew { get; private set; }
    public string? Passengers { get; private set; }
    public string? PilotingCheck { get; private set; }
    public int? ArmorClass { get; private set; }
    public int? Fortitude { get; private set; }
    public int? Hardness { get; private set; }
    public int? HitPoints { get; private set; }
    public int? BrokenThreshold { get; private set; }
    public string? Immunities { get; private set; }
    public string? Speed { get; private set; }
    public string? Collision { get; private set; }
    public string? AbilitiesText { get; private set; }
    public string Source { get; private set; } = "";

    private Pf2eVehicle() { }

    public static Pf2eVehicle Create(
        string slug, string name, string nameRu, int level, string? size, string? price,
        string? dimensions, string? crew, string? passengers, string? pilotingCheck,
        int? armorClass, int? fortitude, int? hardness, int? hitPoints, int? brokenThreshold,
        string? immunities, string? speed, string? collision, string? abilitiesText, string source)
    {
        return new Pf2eVehicle
        {
            Id = Pf2eVehicleId.New(),
            Slug = slug,
            Name = name,
            NameRu = nameRu,
            Level = level,
            Size = size,
            Price = price,
            Dimensions = dimensions,
            Crew = crew,
            Passengers = passengers,
            PilotingCheck = pilotingCheck,
            ArmorClass = armorClass,
            Fortitude = fortitude,
            Hardness = hardness,
            HitPoints = hitPoints,
            BrokenThreshold = brokenThreshold,
            Immunities = immunities,
            Speed = speed,
            Collision = collision,
            AbilitiesText = abilitiesText,
            Source = source,
        };
    }
}
