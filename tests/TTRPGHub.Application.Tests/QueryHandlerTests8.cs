using NSubstitute;
using TTRPGHub.Entities.Pf2e;
using TTRPGHub.Features.Pf2e.Hazards.Queries.GetHazardDetail;
using TTRPGHub.Features.Pf2e.Hazards.Queries.GetHazards;
using TTRPGHub.Features.Pf2e.Monsters.Queries.GetMonsterDetail;
using TTRPGHub.Features.Pf2e.Monsters.Queries.GetMonsters;
using TTRPGHub.Features.Pf2e.Spells.Queries.GetSpellDetail;
using TTRPGHub.Features.Pf2e.Spells.Queries.GetSpells;
using TTRPGHub.Features.Pf2e.Vehicles.Queries.GetVehicleDetail;
using TTRPGHub.Features.Pf2e.Vehicles.Queries.GetVehicles;
using TTRPGHub.Repositories.Pf2e;

namespace TTRPGHub.Application.Tests;

public class GetPf2eMonstersQueryHandlerTests
{
    private readonly IPf2eMonsterRepository _repository = Substitute.For<IPf2eMonsterRepository>();

    private GetPf2eMonstersQueryHandler CreateHandler() => new(_repository);

    private static Pf2eMonster CreateMonster() => Pf2eMonster.Create(
        "goblin-warrior", "Goblin Warrior", 1, "Small", "goblinoid,humanoid",
        6, null, null, null, 0, 3, 1, -1, 0, 0,
        15, 5, 8, 3, 6, "25 feet", null, null, "Pathfinder Bestiary");

    [Fact]
    public async Task Handle_ReturnsPagedMonsterSummaries()
    {
        var monster = CreateMonster();
        _repository.SearchAsync(null, null, null, null, 1, 30).Returns(([monster], 1));
        var handler = CreateHandler();

        var result = await handler.Handle(new GetPf2eMonstersQuery(null, null, null, null), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Items);
        Assert.Equal(1, result.Value.Total);
    }
}

public class GetPf2eMonsterDetailQueryHandlerTests
{
    private readonly IPf2eMonsterRepository _repository = Substitute.For<IPf2eMonsterRepository>();

    private GetPf2eMonsterDetailQueryHandler CreateHandler() => new(_repository);

    [Fact]
    public async Task Handle_NotFound_ReturnsNotFound()
    {
        _repository.GetByIdAsync(Arg.Any<Pf2eMonsterId>()).Returns((Pf2eMonster?)null);
        var handler = CreateHandler();

        var result = await handler.Handle(new GetPf2eMonsterDetailQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_Found_ReturnsDetail()
    {
        var monster = Pf2eMonster.Create(
            "goblin-warrior", "Goblin Warrior", 1, "Small", "goblinoid,humanoid",
            6, null, null, null, 0, 3, 1, -1, 0, 0,
            15, 5, 8, 3, 6, "25 feet", null, null, "Pathfinder Bestiary");
        _repository.GetByIdAsync(Arg.Any<Pf2eMonsterId>()).Returns(monster);
        var handler = CreateHandler();

        var result = await handler.Handle(new GetPf2eMonsterDetailQuery(monster.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Goblin Warrior", result.Value!.Name);
    }
}

public class GetPf2eSpellsQueryHandlerTests
{
    private readonly IPf2eSpellRepository _repository = Substitute.For<IPf2eSpellRepository>();

    private GetPf2eSpellsQueryHandler CreateHandler() => new(_repository);

    [Fact]
    public async Task Handle_ReturnsPagedSpellSummaries()
    {
        var spell = Pf2eSpell.Create(
            "acid-arrow", "Acid Arrow", 2, "arcane,primal", "acid,attack",
            "2", "120 feet", null, "one creature", "sustained up to 1 minute",
            "You shoot a magical arrow of acid.", null, "Pathfinder Core Rulebook");
        _repository.SearchAsync(null, null, null, null, 1, 30).Returns(([spell], 1));
        var handler = CreateHandler();

        var result = await handler.Handle(new GetPf2eSpellsQuery(null, null, null, null), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Items);
    }
}

public class GetPf2eSpellDetailQueryHandlerTests
{
    private readonly IPf2eSpellRepository _repository = Substitute.For<IPf2eSpellRepository>();

    private GetPf2eSpellDetailQueryHandler CreateHandler() => new(_repository);

    [Fact]
    public async Task Handle_NotFound_ReturnsNotFound()
    {
        _repository.GetByIdAsync(Arg.Any<Pf2eSpellId>()).Returns((Pf2eSpell?)null);
        var handler = CreateHandler();

        var result = await handler.Handle(new GetPf2eSpellDetailQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
    }
}

public class GetPf2eHazardsQueryHandlerTests
{
    private readonly IPf2eHazardRepository _repository = Substitute.For<IPf2eHazardRepository>();

    private GetPf2eHazardsQueryHandler CreateHandler() => new(_repository);

    [Fact]
    public async Task Handle_ReturnsPagedHazardSummaries()
    {
        var hazard = Pf2eHazard.Create(
            "spring-loaded-trap", "Spring-Loaded Trap", "Пружинный капкан", 1, "trap",
            15, null, null, null, null, null, null, null, null,
            null, null, null, "Pathfinder GM Core");
        _repository.SearchAsync(null, null, 1, 30).Returns(([hazard], 1));
        var handler = CreateHandler();

        var result = await handler.Handle(new GetPf2eHazardsQuery(null, null), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Items);
    }
}

public class GetPf2eHazardDetailQueryHandlerTests
{
    private readonly IPf2eHazardRepository _repository = Substitute.For<IPf2eHazardRepository>();

    private GetPf2eHazardDetailQueryHandler CreateHandler() => new(_repository);

    [Fact]
    public async Task Handle_NotFound_ReturnsNotFound()
    {
        _repository.GetByIdAsync(Arg.Any<Pf2eHazardId>()).Returns((Pf2eHazard?)null);
        var handler = CreateHandler();

        var result = await handler.Handle(new GetPf2eHazardDetailQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
    }
}

public class GetPf2eVehiclesQueryHandlerTests
{
    private readonly IPf2eVehicleRepository _repository = Substitute.For<IPf2eVehicleRepository>();

    private GetPf2eVehiclesQueryHandler CreateHandler() => new(_repository);

    [Fact]
    public async Task Handle_ReturnsPagedVehicleSummaries()
    {
        var vehicle = Pf2eVehicle.Create(
            "sailing-ship", "Sailing Ship", "Парусный корабль", 5, "Gargantuan", null,
            null, null, null, null, 20, 12, 10, 100, null,
            null, "30 feet", null, null, "Pathfinder GM Core");
        _repository.SearchAsync(null, null, 1, 30).Returns(([vehicle], 1));
        var handler = CreateHandler();

        var result = await handler.Handle(new GetPf2eVehiclesQuery(null, null), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Items);
    }
}

public class GetPf2eVehicleDetailQueryHandlerTests
{
    private readonly IPf2eVehicleRepository _repository = Substitute.For<IPf2eVehicleRepository>();

    private GetPf2eVehicleDetailQueryHandler CreateHandler() => new(_repository);

    [Fact]
    public async Task Handle_NotFound_ReturnsNotFound()
    {
        _repository.GetByIdAsync(Arg.Any<Pf2eVehicleId>()).Returns((Pf2eVehicle?)null);
        var handler = CreateHandler();

        var result = await handler.Handle(new GetPf2eVehicleDetailQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
    }
}
