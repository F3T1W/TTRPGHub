using NSubstitute;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Features.SessionNotes.Commands.CreateNote;
using TTRPGHub.Features.SessionNotes.Commands.DeleteNote;
using TTRPGHub.Features.SessionNotes.Commands.UpdateNote;
using TTRPGHub.Repositories;

namespace TTRPGHub.Application.Tests;

public class CreateNoteCommandHandlerTests
{
    private readonly ISessionNoteRepository _noteRepository = Substitute.For<ISessionNoteRepository>();
    private readonly ICampaignRepository _campaignRepository = Substitute.For<ICampaignRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    private CreateNoteCommandHandler CreateHandler() => new(_noteRepository, _campaignRepository, _unitOfWork, _currentUser);

    private static CreateNoteCommand ValidCommand(Guid campaignId) => new(
        campaignId, "Session 3 recap", "Party defeated the goblin chief", DateTime.UtcNow);

    [Fact]
    public async Task Handle_CampaignNotFound_ReturnsNotFound()
    {
        _campaignRepository.GetByIdAsync(Arg.Any<CampaignId>()).Returns((Campaign?)null);
        var handler = CreateHandler();

        var result = await handler.Handle(ValidCommand(Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_NonParticipant_ReturnsUnauthorized()
    {
        var campaign = Campaign.Create(UserId.New(), "Test", null, "pf2e");
        _campaignRepository.GetByIdAsync(Arg.Any<CampaignId>()).Returns(campaign);
        _currentUser.Id.Returns(UserId.New());
        var handler = CreateHandler();

        var result = await handler.Handle(ValidCommand(campaign.Id.Value), CancellationToken.None);

        Assert.True(result.IsFailure);
        await _noteRepository.DidNotReceive().AddAsync(Arg.Any<SessionNote>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Organizer_CreatesNote()
    {
        var organizerId = UserId.New();
        var campaign = Campaign.Create(organizerId, "Test", null, "pf2e");
        _campaignRepository.GetByIdAsync(Arg.Any<CampaignId>()).Returns(campaign);
        _currentUser.Id.Returns(organizerId);
        var handler = CreateHandler();

        var result = await handler.Handle(ValidCommand(campaign.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        await _noteRepository.Received(1).AddAsync(
            Arg.Is<SessionNote>(n => n.CampaignId == campaign.Id && n.AuthorId == organizerId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_RegularParticipant_CanAlsoCreateNote()
    {
        var organizerId = UserId.New();
        var participantId = UserId.New();
        var campaign = Campaign.Create(organizerId, "Test", null, "pf2e");
        campaign.AddParticipant(participantId);
        _campaignRepository.GetByIdAsync(Arg.Any<CampaignId>()).Returns(campaign);
        _currentUser.Id.Returns(participantId);
        var handler = CreateHandler();

        var result = await handler.Handle(ValidCommand(campaign.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
    }
}

public class UpdateNoteCommandHandlerTests
{
    private readonly ISessionNoteRepository _noteRepository = Substitute.For<ISessionNoteRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    private UpdateNoteCommandHandler CreateHandler() => new(_noteRepository, _unitOfWork, _currentUser);

    private static SessionNote CreateNote(UserId authorId) =>
        SessionNote.Create(CampaignId.New(), authorId, "Old title", "Old content", DateTime.UtcNow.AddDays(-1));

    [Fact]
    public async Task Handle_NoteNotFound_ReturnsNotFound()
    {
        _noteRepository.GetByIdAsync(Arg.Any<SessionNoteId>()).Returns((SessionNote?)null);
        var handler = CreateHandler();

        var result = await handler.Handle(
            new UpdateNoteCommand(Guid.NewGuid(), "New title", "New content", DateTime.UtcNow), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_NonAuthor_ReturnsUnauthorized()
    {
        var note = CreateNote(UserId.New());
        _noteRepository.GetByIdAsync(Arg.Any<SessionNoteId>()).Returns(note);
        _currentUser.Id.Returns(UserId.New());
        var handler = CreateHandler();

        var result = await handler.Handle(
            new UpdateNoteCommand(note.Id.Value, "New title", "New content", DateTime.UtcNow), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Old title", note.Title);
    }

    [Fact]
    public async Task Handle_Author_UpdatesNote()
    {
        var authorId = UserId.New();
        var note = CreateNote(authorId);
        _noteRepository.GetByIdAsync(Arg.Any<SessionNoteId>()).Returns(note);
        _currentUser.Id.Returns(authorId);
        var handler = CreateHandler();
        var newDate = DateTime.UtcNow;

        var result = await handler.Handle(
            new UpdateNoteCommand(note.Id.Value, "New title", "New content", newDate), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("New title", note.Title);
        Assert.Equal("New content", note.Content);
        Assert.Equal(newDate, note.SessionDate);
        _noteRepository.Received(1).Update(note);
    }
}

public class DeleteNoteCommandHandlerTests
{
    private readonly ISessionNoteRepository _noteRepository = Substitute.For<ISessionNoteRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    private DeleteNoteCommandHandler CreateHandler() => new(_noteRepository, _unitOfWork, _currentUser);

    private static SessionNote CreateNote(UserId authorId) =>
        SessionNote.Create(CampaignId.New(), authorId, "Test", "Content", DateTime.UtcNow);

    [Fact]
    public async Task Handle_NonAuthor_ReturnsUnauthorized()
    {
        var note = CreateNote(UserId.New());
        _noteRepository.GetByIdAsync(Arg.Any<SessionNoteId>()).Returns(note);
        _currentUser.Id.Returns(UserId.New());
        var handler = CreateHandler();

        var result = await handler.Handle(new DeleteNoteCommand(note.Id.Value), CancellationToken.None);

        Assert.True(result.IsFailure);
        _noteRepository.DidNotReceive().Delete(Arg.Any<SessionNote>());
    }

    [Fact]
    public async Task Handle_Author_DeletesNote()
    {
        var authorId = UserId.New();
        var note = CreateNote(authorId);
        _noteRepository.GetByIdAsync(Arg.Any<SessionNoteId>()).Returns(note);
        _currentUser.Id.Returns(authorId);
        var handler = CreateHandler();

        var result = await handler.Handle(new DeleteNoteCommand(note.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        _noteRepository.Received(1).Delete(note);
    }
}
