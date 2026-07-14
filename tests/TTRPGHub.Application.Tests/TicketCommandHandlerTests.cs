using NSubstitute;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Features.Tickets.Commands.AddTicketComment;
using TTRPGHub.Features.Tickets.Commands.ChangeTicketStatus;
using TTRPGHub.Features.Tickets.Commands.CreateTicket;
using TTRPGHub.Repositories;

namespace TTRPGHub.Application.Tests;

public class CreateTicketCommandHandlerTests
{
    private readonly ISupportTicketRepository _tickets = Substitute.For<ISupportTicketRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IStorageService _storage = Substitute.For<IStorageService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private CreateTicketCommandHandler CreateHandler() => new(_tickets, _currentUser, _storage, _unitOfWork);

    [Fact]
    public async Task Handle_FileTooLarge_ReturnsValidationError()
    {
        var oversized = new TicketFileUpload(Stream.Null, "huge.zip", "application/zip", 11 * 1024 * 1024);
        var handler = CreateHandler();

        var result = await handler.Handle(
            new CreateTicketCommand("Bug", "Description", null, [oversized]), CancellationToken.None);

        Assert.True(result.IsFailure);
        await _storage.DidNotReceive().EnsureBucketExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NoFiles_SkipsStorage()
    {
        var reporterId = UserId.New();
        _currentUser.Id.Returns(reporterId);
        var handler = CreateHandler();

        var result = await handler.Handle(new CreateTicketCommand("Bug", "Description", null, []), CancellationToken.None);

        Assert.True(result.IsSuccess);
        await _storage.DidNotReceive().EnsureBucketExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _tickets.Received(1).AddAsync(
            Arg.Is<SupportTicket>(t => t.ReporterId == reporterId), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithFiles_UploadsAndAttaches()
    {
        _currentUser.Id.Returns(UserId.New());
        _storage.UploadAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("https://storage.test/tickets/screenshot.png");
        var file = new TicketFileUpload(Stream.Null, "screenshot.png", "image/png", 1024);
        SupportTicket? captured = null;
        _tickets.When(t => t.AddAsync(Arg.Any<SupportTicket>(), Arg.Any<CancellationToken>()))
            .Do(call => captured = call.Arg<SupportTicket>());
        var handler = CreateHandler();

        var result = await handler.Handle(new CreateTicketCommand("Bug", "Description", null, [file]), CancellationToken.None);

        Assert.True(result.IsSuccess);
        await _storage.Received(1).EnsureBucketExistsAsync("tickets", Arg.Any<CancellationToken>());
        Assert.Single(captured!.Attachments);
        Assert.Equal("screenshot.png", captured.Attachments[0].FileName);
    }
}

public class AddTicketCommentCommandHandlerTests
{
    private readonly ISupportTicketRepository _tickets = Substitute.For<ISupportTicketRepository>();
    private readonly ITicketCommentRepository _comments = Substitute.For<ITicketCommentRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private AddTicketCommentCommandHandler CreateHandler() => new(_tickets, _comments, _currentUser, _unitOfWork);

    [Fact]
    public async Task Handle_BlankBody_ReturnsValidationError()
    {
        var handler = CreateHandler();

        var result = await handler.Handle(new AddTicketCommentCommand(Guid.NewGuid(), "   "), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_TicketNotFound_ReturnsNotFound()
    {
        _tickets.GetByIdAsync(Arg.Any<SupportTicketId>()).Returns((SupportTicket?)null);
        var handler = CreateHandler();

        var result = await handler.Handle(new AddTicketCommentCommand(Guid.NewGuid(), "Hello"), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_UnrelatedPlayer_ReturnsForbidden()
    {
        var ticket = SupportTicket.Create(UserId.New(), "Bug", "Description", null);
        _tickets.GetByIdAsync(Arg.Any<SupportTicketId>()).Returns(ticket);
        _currentUser.Id.Returns(UserId.New());
        _currentUser.Role.Returns(UserRole.Player);
        var handler = CreateHandler();

        var result = await handler.Handle(new AddTicketCommentCommand(ticket.Id.Value, "Hello"), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_TicketReporter_CanComment()
    {
        var reporterId = UserId.New();
        var ticket = SupportTicket.Create(reporterId, "Bug", "Description", null);
        _tickets.GetByIdAsync(Arg.Any<SupportTicketId>()).Returns(ticket);
        _currentUser.Id.Returns(reporterId);
        _currentUser.Role.Returns(UserRole.Player);
        var handler = CreateHandler();

        var result = await handler.Handle(new AddTicketCommentCommand(ticket.Id.Value, "More info here"), CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_ModeratorNotInvolvedInTicket_CanStillComment()
    {
        var ticket = SupportTicket.Create(UserId.New(), "Bug", "Description", null);
        _tickets.GetByIdAsync(Arg.Any<SupportTicketId>()).Returns(ticket);
        _currentUser.Id.Returns(UserId.New());
        _currentUser.Role.Returns(UserRole.Moderator);
        var handler = CreateHandler();

        var result = await handler.Handle(new AddTicketCommentCommand(ticket.Id.Value, "We're looking into it"), CancellationToken.None);

        Assert.True(result.IsSuccess);
    }
}

public class ChangeTicketStatusCommandHandlerTests
{
    private readonly ISupportTicketRepository _tickets = Substitute.For<ISupportTicketRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private ChangeTicketStatusCommandHandler CreateHandler() => new(_tickets, _unitOfWork);

    [Fact]
    public async Task Handle_InvalidStatus_ReturnsValidationError()
    {
        var handler = CreateHandler();

        var result = await handler.Handle(new ChangeTicketStatusCommand(Guid.NewGuid(), "Archived"), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_TicketNotFound_ReturnsNotFound()
    {
        _tickets.GetByIdAsync(Arg.Any<SupportTicketId>()).Returns((SupportTicket?)null);
        var handler = CreateHandler();

        var result = await handler.Handle(new ChangeTicketStatusCommand(Guid.NewGuid(), "Done"), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_ValidStatus_UpdatesTicket()
    {
        var ticket = SupportTicket.Create(UserId.New(), "Bug", "Description", null);
        _tickets.GetByIdAsync(Arg.Any<SupportTicketId>()).Returns(ticket);
        var handler = CreateHandler();

        var result = await handler.Handle(new ChangeTicketStatusCommand(ticket.Id.Value, "InProgress"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(TicketStatus.InProgress, ticket.Status);
    }
}
