using NSubstitute;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Entities.Events;
using TTRPGHub.Features.Events.Commands.CancelEvent;
using TTRPGHub.Features.Events.Commands.CreateEvent;
using TTRPGHub.Features.Events.Commands.RegisterForEvent;
using TTRPGHub.Features.Events.Commands.UnregisterFromEvent;
using TTRPGHub.Repositories;

namespace TTRPGHub.Application.Tests;

public class CreateEventCommandHandlerTests
{
    private readonly IGameEventRepository _repo = Substitute.For<IGameEventRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private CreateEventCommandHandler CreateHandler() => new(_repo, _currentUser, _unitOfWork);

    private static CreateEventCommand ValidCommand(string format = "Online") => new(
        "Открытый стол PF2e", "Приходите с листом персонажа", "pf2e", format, null, "https://discord.gg/test",
        DateTime.UtcNow.AddDays(3), 6);

    [Fact]
    public async Task Handle_InvalidFormat_ReturnsValidationError()
    {
        var handler = CreateHandler();

        var result = await handler.Handle(ValidCommand("Virtual"), CancellationToken.None);

        Assert.True(result.IsFailure);
        await _repo.DidNotReceive().AddAsync(Arg.Any<GameEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidFormat_CreatesEventOwnedByCurrentUser()
    {
        var organizerId = UserId.New();
        _currentUser.Id.Returns(organizerId);
        var handler = CreateHandler();

        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        await _repo.Received(1).AddAsync(
            Arg.Is<GameEvent>(e => e.OrganizerId == organizerId && e.Format == EventFormat.Online),
            Arg.Any<CancellationToken>());
    }
}

public class CancelEventCommandHandlerTests
{
    private readonly IGameEventRepository _repo = Substitute.For<IGameEventRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private CancelEventCommandHandler CreateHandler() => new(_repo, _currentUser, _unitOfWork);

    private static GameEvent CreateEvent(UserId organizerId) => GameEvent.Create(
        organizerId, "Test", null, "pf2e", EventFormat.Online, null, null, DateTime.UtcNow.AddDays(1), 4);

    [Fact]
    public async Task Handle_EventNotFound_ReturnsNotFound()
    {
        _repo.GetByIdWithParticipantsAsync(Arg.Any<GameEventId>()).Returns((GameEvent?)null);
        var handler = CreateHandler();

        var result = await handler.Handle(new CancelEventCommand(Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_NonOrganizer_ReturnsForbidden()
    {
        var ev = CreateEvent(UserId.New());
        _repo.GetByIdWithParticipantsAsync(Arg.Any<GameEventId>()).Returns(ev);
        _currentUser.Id.Returns(UserId.New());
        var handler = CreateHandler();

        var result = await handler.Handle(new CancelEventCommand(ev.Id.Value), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.False(ev.IsCancelled);
    }

    [Fact]
    public async Task Handle_Organizer_CancelsEvent()
    {
        var organizerId = UserId.New();
        var ev = CreateEvent(organizerId);
        _repo.GetByIdWithParticipantsAsync(Arg.Any<GameEventId>()).Returns(ev);
        _currentUser.Id.Returns(organizerId);
        var handler = CreateHandler();

        var result = await handler.Handle(new CancelEventCommand(ev.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(ev.IsCancelled);
    }
}

public class RegisterForEventCommandHandlerTests
{
    private readonly IGameEventRepository _repo = Substitute.For<IGameEventRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private RegisterForEventCommandHandler CreateHandler() => new(_repo, _currentUser, _unitOfWork);

    private static GameEvent CreateEvent(DateTime? startsAt = null, int maxParticipants = 4) => GameEvent.Create(
        UserId.New(), "Test", null, "pf2e", EventFormat.Online, null, null,
        startsAt ?? DateTime.UtcNow.AddDays(1), maxParticipants);

    [Fact]
    public async Task Handle_EventNotFound_ReturnsNotFound()
    {
        _repo.GetByIdWithParticipantsAsync(Arg.Any<GameEventId>()).Returns((GameEvent?)null);
        var handler = CreateHandler();

        var result = await handler.Handle(new RegisterForEventCommand(Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_CancelledEvent_ReturnsValidationError()
    {
        var ev = CreateEvent();
        ev.Cancel();
        _repo.GetByIdWithParticipantsAsync(Arg.Any<GameEventId>()).Returns(ev);
        _currentUser.Id.Returns(UserId.New());
        var handler = CreateHandler();

        var result = await handler.Handle(new RegisterForEventCommand(ev.Id.Value), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_PastEvent_ReturnsValidationError()
    {
        var ev = CreateEvent(DateTime.UtcNow.AddDays(-1));
        _repo.GetByIdWithParticipantsAsync(Arg.Any<GameEventId>()).Returns(ev);
        _currentUser.Id.Returns(UserId.New());
        var handler = CreateHandler();

        var result = await handler.Handle(new RegisterForEventCommand(ev.Id.Value), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_AlreadyRegistered_ReturnsConflict()
    {
        var ev = CreateEvent();
        var userId = UserId.New();
        ev.AddParticipant(userId);
        _repo.GetByIdWithParticipantsAsync(Arg.Any<GameEventId>()).Returns(ev);
        _currentUser.Id.Returns(userId);
        var handler = CreateHandler();

        var result = await handler.Handle(new RegisterForEventCommand(ev.Id.Value), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_NoSlotsLeft_ReturnsValidationError()
    {
        var ev = CreateEvent(maxParticipants: 1);
        ev.AddParticipant(UserId.New());
        _repo.GetByIdWithParticipantsAsync(Arg.Any<GameEventId>()).Returns(ev);
        _currentUser.Id.Returns(UserId.New());
        var handler = CreateHandler();

        var result = await handler.Handle(new RegisterForEventCommand(ev.Id.Value), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_ValidRegistration_AddsParticipant()
    {
        var ev = CreateEvent();
        var userId = UserId.New();
        _repo.GetByIdWithParticipantsAsync(Arg.Any<GameEventId>()).Returns(ev);
        _currentUser.Id.Returns(userId);
        var handler = CreateHandler();

        var result = await handler.Handle(new RegisterForEventCommand(ev.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(ev.IsParticipant(userId));
    }
}

public class UnregisterFromEventCommandHandlerTests
{
    private readonly IGameEventRepository _repo = Substitute.For<IGameEventRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private UnregisterFromEventCommandHandler CreateHandler() => new(_repo, _currentUser, _unitOfWork);

    private static GameEvent CreateEvent() => GameEvent.Create(
        UserId.New(), "Test", null, "pf2e", EventFormat.Online, null, null, DateTime.UtcNow.AddDays(1), 4);

    [Fact]
    public async Task Handle_NotRegistered_ReturnsNotFound()
    {
        var ev = CreateEvent();
        _repo.GetByIdWithParticipantsAsync(Arg.Any<GameEventId>()).Returns(ev);
        _currentUser.Id.Returns(UserId.New());
        var handler = CreateHandler();

        var result = await handler.Handle(new UnregisterFromEventCommand(ev.Id.Value), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_Registered_RemovesParticipant()
    {
        var ev = CreateEvent();
        var userId = UserId.New();
        ev.AddParticipant(userId);
        _repo.GetByIdWithParticipantsAsync(Arg.Any<GameEventId>()).Returns(ev);
        _currentUser.Id.Returns(userId);
        var handler = CreateHandler();

        var result = await handler.Handle(new UnregisterFromEventCommand(ev.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(ev.IsParticipant(userId));
    }
}
