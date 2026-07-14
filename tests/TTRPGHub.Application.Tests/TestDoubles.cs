using NSubstitute;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Features.Initiative.Shared;
using TTRPGHub.Repositories;

namespace TTRPGHub.Application.Tests;

// InitiativeTrackerSync is a concrete `internal sealed class` (not an interface), so it can't be
// substituted directly. It's cheap and safe to construct for real with all-mocked dependencies —
// GetByLinkedSessionAsync defaults to an empty list, so PushTokenAsync becomes a no-op unless a
// test explicitly wires up a linked tracker.
internal static class TestDoubles
{
    internal static InitiativeTrackerSync CreateInertTrackerSync(
        IInitiativeTrackerRepository? trackerRepository = null,
        ITableTokenRepository? tokenRepository = null,
        IGameSessionRepository? sessionRepository = null,
        IUnitOfWork? unitOfWork = null,
        ITrackerNotifier? trackerNotifier = null,
        ITableNotifier? tableNotifier = null)
    {
        trackerRepository ??= Substitute.For<IInitiativeTrackerRepository>();
        trackerRepository.GetByLinkedSessionAsync(Arg.Any<Guid>())
            .Returns((IReadOnlyList<InitiativeTracker>)[]);

        return new InitiativeTrackerSync(
            trackerRepository,
            tokenRepository ?? Substitute.For<ITableTokenRepository>(),
            sessionRepository ?? Substitute.For<IGameSessionRepository>(),
            unitOfWork ?? Substitute.For<IUnitOfWork>(),
            trackerNotifier ?? Substitute.For<ITrackerNotifier>(),
            tableNotifier ?? Substitute.For<ITableNotifier>());
    }
}
