using NSubstitute;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities.Moderation;
using TTRPGHub.Features.Moderation.Commands.CreateReport;
using TTRPGHub.Features.Moderation.Commands.ResolveReport;
using TTRPGHub.Repositories;

namespace TTRPGHub.Application.Tests;

public class CreateReportCommandHandlerTests
{
    private readonly IContentReportRepository _reports = Substitute.For<IContentReportRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private CreateReportCommandHandler CreateHandler() => new(_reports, _currentUser, _unitOfWork);

    [Fact]
    public async Task Handle_CreatesReportOwnedByReporter()
    {
        var reporterId = TTRPGHub.Entities.UserId.New();
        _currentUser.Id.Returns(reporterId);
        var entityId = Guid.NewGuid();
        var handler = CreateHandler();

        var result = await handler.Handle(
            new CreateReportCommand(ReportedEntityType.ForumPost, entityId, "Spam"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        await _reports.Received(1).AddAsync(
            Arg.Is<ContentReport>(r =>
                r.ReporterId == reporterId && r.EntityId == entityId &&
                r.EntityType == ReportedEntityType.ForumPost && r.Status == ReportStatus.Open),
            Arg.Any<CancellationToken>());
    }
}

public class ResolveReportCommandHandlerTests
{
    private readonly IContentReportRepository _reports = Substitute.For<IContentReportRepository>();
    private readonly IModerationLogRepository _moderationLog = Substitute.For<IModerationLogRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private ResolveReportCommandHandler CreateHandler() => new(_reports, _moderationLog, _currentUser, _unitOfWork);

    [Fact]
    public async Task Handle_OpenStatus_IsRejected()
    {
        var handler = CreateHandler();

        // "Open" is the starting state, not something a moderator resolves a report *into*.
        var result = await handler.Handle(new ResolveReportCommand(Guid.NewGuid(), "Open"), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_UnknownStatus_ReturnsValidationError()
    {
        var handler = CreateHandler();

        var result = await handler.Handle(new ResolveReportCommand(Guid.NewGuid(), "Banned"), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_ReportNotFound_ReturnsNotFound()
    {
        _reports.GetByIdAsync(Arg.Any<ContentReportId>()).Returns((ContentReport?)null);
        var handler = CreateHandler();

        var result = await handler.Handle(new ResolveReportCommand(Guid.NewGuid(), "Resolved"), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_ValidResolution_UpdatesReportAndLogsAction()
    {
        var report = ContentReport.Create(TTRPGHub.Entities.UserId.New(), ReportedEntityType.HomebrewItem, Guid.NewGuid(), "Offensive content");
        _reports.GetByIdAsync(Arg.Any<ContentReportId>()).Returns(report);
        var moderatorId = TTRPGHub.Entities.UserId.New();
        _currentUser.Id.Returns(moderatorId);
        var handler = CreateHandler();

        var result = await handler.Handle(new ResolveReportCommand(report.Id.Value, "Dismissed"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(ReportStatus.Dismissed, report.Status);
        await _moderationLog.Received(1).AddAsync(
            Arg.Is<ModerationLogEntry>(e => e.ActorUserId == moderatorId && e.Action == "ResolveReport:Dismissed"),
            Arg.Any<CancellationToken>());
    }
}
