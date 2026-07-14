using NSubstitute;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Features.Calendar.Commands.SubscribePush;
using TTRPGHub.Features.Calendar.Commands.UnsubscribePush;
using TTRPGHub.Features.Calendar.Commands.UpsertCalendarPreference;
using TTRPGHub.Repositories;

namespace TTRPGHub.Application.Tests;

public class SubscribePushCommandHandlerTests
{
    private readonly IPushSubscriptionRepository _repository = Substitute.For<IPushSubscriptionRepository>();
    private readonly IUserCalendarPreferenceRepository _preferenceRepository = Substitute.For<IUserCalendarPreferenceRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    private SubscribePushCommandHandler CreateHandler() =>
        new(_repository, _preferenceRepository, _unitOfWork, _currentUser);

    private static SubscribePushCommand ValidCommand() => new("https://push.test/endpoint", "p256dh-key", "auth-secret");

    [Fact]
    public async Task Handle_Unauthenticated_ReturnsUnauthorized()
    {
        _currentUser.IsAuthenticated.Returns(false);
        var handler = CreateHandler();

        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.True(result.IsFailure);
        await _repository.DidNotReceive().AddAsync(Arg.Any<PushSubscription>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NewEndpoint_AddsSubscription()
    {
        _currentUser.IsAuthenticated.Returns(true);
        var userId = UserId.New();
        _currentUser.Id.Returns(userId);
        _repository.GetByEndpointAsync(Arg.Any<string>()).Returns((PushSubscription?)null);
        _preferenceRepository.GetByUserIdAsync(Arg.Any<UserId>()).Returns((UserCalendarPreference?)null);
        var handler = CreateHandler();

        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        await _repository.Received(1).AddAsync(
            Arg.Is<PushSubscription>(s => s.UserId == userId && s.Endpoint == "https://push.test/endpoint"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AlreadySubscribedEndpoint_DoesNotDuplicate()
    {
        _currentUser.IsAuthenticated.Returns(true);
        var userId = UserId.New();
        _currentUser.Id.Returns(userId);
        _repository.GetByEndpointAsync(Arg.Any<string>())
            .Returns(PushSubscription.Create(userId, "https://push.test/endpoint", "p256dh-key", "auth-secret"));
        _preferenceRepository.GetByUserIdAsync(Arg.Any<UserId>()).Returns((UserCalendarPreference?)null);
        var handler = CreateHandler();

        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        await _repository.DidNotReceive().AddAsync(Arg.Any<PushSubscription>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NoExistingPreference_CreatesOneWithPushEnabled()
    {
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.Id.Returns(UserId.New());
        _repository.GetByEndpointAsync(Arg.Any<string>()).Returns((PushSubscription?)null);
        _preferenceRepository.GetByUserIdAsync(Arg.Any<UserId>()).Returns((UserCalendarPreference?)null);
        var handler = CreateHandler();

        await handler.Handle(ValidCommand(), CancellationToken.None);

        await _preferenceRepository.Received(1).AddAsync(
            Arg.Is<UserCalendarPreference>(p => p.PushEnabled), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ExistingPreferenceWithPushDisabled_EnablesIt()
    {
        _currentUser.IsAuthenticated.Returns(true);
        var userId = UserId.New();
        _currentUser.Id.Returns(userId);
        _repository.GetByEndpointAsync(Arg.Any<string>()).Returns((PushSubscription?)null);
        var pref = UserCalendarPreference.Create(userId, 60);
        _preferenceRepository.GetByUserIdAsync(Arg.Any<UserId>()).Returns(pref);
        var handler = CreateHandler();

        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(pref.PushEnabled);
        _preferenceRepository.Received(1).Update(pref);
    }
}

public class UnsubscribePushCommandHandlerTests
{
    private readonly IPushSubscriptionRepository _repository = Substitute.For<IPushSubscriptionRepository>();
    private readonly IUserCalendarPreferenceRepository _preferenceRepository = Substitute.For<IUserCalendarPreferenceRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    private UnsubscribePushCommandHandler CreateHandler() =>
        new(_repository, _preferenceRepository, _unitOfWork, _currentUser);

    [Fact]
    public async Task Handle_Unauthenticated_ReturnsUnauthorized()
    {
        _currentUser.IsAuthenticated.Returns(false);
        var handler = CreateHandler();

        var result = await handler.Handle(new UnsubscribePushCommand("https://push.test/endpoint"), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_LastSubscription_DisablesPushPreference()
    {
        _currentUser.IsAuthenticated.Returns(true);
        var userId = UserId.New();
        _currentUser.Id.Returns(userId);
        var subscription = PushSubscription.Create(userId, "https://push.test/endpoint", "key", "secret");
        _repository.GetByEndpointAsync(Arg.Any<string>()).Returns(subscription);
        _repository.GetByUserIdAsync(Arg.Any<UserId>()).Returns((IReadOnlyList<PushSubscription>)[]);
        var pref = UserCalendarPreference.Create(userId, 60);
        pref.SetPushEnabled(true);
        _preferenceRepository.GetByUserIdAsync(Arg.Any<UserId>()).Returns(pref);
        var handler = CreateHandler();

        var result = await handler.Handle(new UnsubscribePushCommand("https://push.test/endpoint"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        _repository.Received(1).Remove(subscription);
        Assert.False(pref.PushEnabled);
        _preferenceRepository.Received(1).Update(pref);
    }

    [Fact]
    public async Task Handle_MultipleOtherSubscriptionsRemain_KeepsPushEnabled()
    {
        // The handler disables push once <= 1 subscription remains, so this needs two survivors
        // to actually verify the "keep enabled" branch.
        _currentUser.IsAuthenticated.Returns(true);
        var userId = UserId.New();
        _currentUser.Id.Returns(userId);
        var subscription = PushSubscription.Create(userId, "https://push.test/endpoint", "key", "secret");
        _repository.GetByEndpointAsync(Arg.Any<string>()).Returns(subscription);
        var others = new List<PushSubscription>
        {
            PushSubscription.Create(userId, "https://push.test/other-1", "key2", "secret2"),
            PushSubscription.Create(userId, "https://push.test/other-2", "key3", "secret3"),
        };
        _repository.GetByUserIdAsync(Arg.Any<UserId>()).Returns((IReadOnlyList<PushSubscription>)others);
        var pref = UserCalendarPreference.Create(userId, 60);
        pref.SetPushEnabled(true);
        _preferenceRepository.GetByUserIdAsync(Arg.Any<UserId>()).Returns(pref);
        var handler = CreateHandler();

        var result = await handler.Handle(new UnsubscribePushCommand("https://push.test/endpoint"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        _preferenceRepository.DidNotReceive().Update(Arg.Any<UserCalendarPreference>());
        Assert.True(pref.PushEnabled);
    }

    [Fact]
    public async Task Handle_UnknownEndpoint_StillSucceeds()
    {
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.Id.Returns(UserId.New());
        _repository.GetByEndpointAsync(Arg.Any<string>()).Returns((PushSubscription?)null);
        _repository.GetByUserIdAsync(Arg.Any<UserId>()).Returns((IReadOnlyList<PushSubscription>)[]);
        _preferenceRepository.GetByUserIdAsync(Arg.Any<UserId>()).Returns((UserCalendarPreference?)null);
        var handler = CreateHandler();

        var result = await handler.Handle(new UnsubscribePushCommand("https://push.test/unknown"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        _repository.DidNotReceive().Remove(Arg.Any<PushSubscription>());
    }
}

public class UpsertCalendarPreferenceCommandHandlerTests
{
    private readonly IUserCalendarPreferenceRepository _repository = Substitute.For<IUserCalendarPreferenceRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    private UpsertCalendarPreferenceCommandHandler CreateHandler() => new(_repository, _unitOfWork, _currentUser);

    [Fact]
    public async Task Handle_Unauthenticated_ReturnsUnauthorized()
    {
        _currentUser.IsAuthenticated.Returns(false);
        var handler = CreateHandler();

        var result = await handler.Handle(new UpsertCalendarPreferenceCommand(30, false), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_NoExistingPreference_CreatesOne()
    {
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.Id.Returns(UserId.New());
        _repository.GetByUserIdAsync(Arg.Any<UserId>()).Returns((UserCalendarPreference?)null);
        var handler = CreateHandler();

        var result = await handler.Handle(new UpsertCalendarPreferenceCommand(30, false), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(30, result.Value!.ReminderMinutes);
        await _repository.Received(1).AddAsync(Arg.Any<UserCalendarPreference>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ExistingPreference_UpdatesReminderWithoutRegeneratingToken()
    {
        var userId = UserId.New();
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.Id.Returns(userId);
        var existing = UserCalendarPreference.Create(userId, 60);
        var originalToken = existing.CalendarToken;
        _repository.GetByUserIdAsync(Arg.Any<UserId>()).Returns(existing);
        var handler = CreateHandler();

        var result = await handler.Handle(new UpsertCalendarPreferenceCommand(15, false), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(15, existing.ReminderMinutes);
        Assert.Equal(originalToken, existing.CalendarToken);
        _repository.Received(1).Update(existing);
    }

    [Fact]
    public async Task Handle_RegenerateTokenRequested_ChangesToken()
    {
        var userId = UserId.New();
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.Id.Returns(userId);
        var existing = UserCalendarPreference.Create(userId, 60);
        var originalToken = existing.CalendarToken;
        _repository.GetByUserIdAsync(Arg.Any<UserId>()).Returns(existing);
        var handler = CreateHandler();

        var result = await handler.Handle(new UpsertCalendarPreferenceCommand(60, true), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotEqual(originalToken, existing.CalendarToken);
        Assert.Equal(existing.CalendarToken, result.Value!.CalendarToken);
    }
}
