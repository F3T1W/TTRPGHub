using NSubstitute;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Features.Users.Commands.ChangeUserRole;
using TTRPGHub.Features.Users.Commands.UpdateProfile;
using TTRPGHub.Repositories;
using TTRPGHub.ValueObjects;

namespace TTRPGHub.Application.Tests;

public class ChangeUserRoleCommandHandlerTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private ChangeUserRoleCommandHandler CreateHandler() => new(_users, _currentUser, _unitOfWork);

    [Fact]
    public async Task Handle_UnknownRole_ReturnsValidationError()
    {
        _currentUser.Id.Returns(UserId.New());
        var handler = CreateHandler();

        var result = await handler.Handle(new ChangeUserRoleCommand(UserId.New().Value, "SuperAdmin"), CancellationToken.None);

        Assert.True(result.IsFailure);
        await _users.DidNotReceive().GetByIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_TargetingSelf_ReturnsValidationError()
    {
        var adminId = UserId.New();
        _currentUser.Id.Returns(adminId);
        var handler = CreateHandler();

        var result = await handler.Handle(new ChangeUserRoleCommand(adminId.Value, "Moderator"), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_TargetUserNotFound_ReturnsNotFound()
    {
        _currentUser.Id.Returns(UserId.New());
        _users.GetByIdAsync(Arg.Any<UserId>()).Returns((User?)null);
        var handler = CreateHandler();

        var result = await handler.Handle(new ChangeUserRoleCommand(UserId.New().Value, "Moderator"), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_ValidRequest_ChangesTargetUsersRole()
    {
        _currentUser.Id.Returns(UserId.New());
        var target = User.Create("someone", Email.Create("someone@test.com").Value!, "hash");
        _users.GetByIdAsync(Arg.Any<UserId>()).Returns(target);
        var handler = CreateHandler();

        var result = await handler.Handle(new ChangeUserRoleCommand(target.Id.Value, "Moderator"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(UserRole.Moderator, target.Role);
        _users.Received(1).Update(target);
    }

    [Fact]
    public async Task Handle_RoleIsCaseInsensitive()
    {
        _currentUser.Id.Returns(UserId.New());
        var target = User.Create("someone", Email.Create("someone@test.com").Value!, "hash");
        _users.GetByIdAsync(Arg.Any<UserId>()).Returns(target);
        var handler = CreateHandler();

        var result = await handler.Handle(new ChangeUserRoleCommand(target.Id.Value, "admin"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(UserRole.Admin, target.Role);
    }
}

public class UpdateProfileCommandHandlerTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private UpdateProfileCommandHandler CreateHandler() => new(_users, _currentUser, _unitOfWork);

    [Fact]
    public async Task Handle_UserNotFound_ReturnsNotFound()
    {
        _currentUser.Id.Returns(UserId.New());
        _users.GetByIdAsync(Arg.Any<UserId>()).Returns((User?)null);
        var handler = CreateHandler();

        var result = await handler.Handle(new UpdateProfileCommand("Name", null, null), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_BlankFields_AreNormalizedToNull()
    {
        var userId = UserId.New();
        _currentUser.Id.Returns(userId);
        var user = User.Create("someone", Email.Create("someone@test.com").Value!, "hash");
        _users.GetByIdAsync(Arg.Any<UserId>()).Returns(user);
        var handler = CreateHandler();

        var result = await handler.Handle(new UpdateProfileCommand("   ", "   ", null), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(user.Profile.DisplayName);
        Assert.Null(user.Profile.Bio);
    }

    [Fact]
    public async Task Handle_CityWithInconsistentCasing_IsNormalizedToTitleCase()
    {
        var userId = UserId.New();
        _currentUser.Id.Returns(userId);
        var user = User.Create("someone", Email.Create("someone@test.com").Value!, "hash");
        _users.GetByIdAsync(Arg.Any<UserId>()).Returns(user);
        var handler = CreateHandler();

        var result = await handler.Handle(new UpdateProfileCommand(null, null, "МОСКВА"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Москва", user.Profile.City);
    }

    [Fact]
    public async Task Handle_CityWithExtraWhitespace_IsCollapsed()
    {
        var userId = UserId.New();
        _currentUser.Id.Returns(userId);
        var user = User.Create("someone", Email.Create("someone@test.com").Value!, "hash");
        _users.GetByIdAsync(Arg.Any<UserId>()).Returns(user);
        var handler = CreateHandler();

        var result = await handler.Handle(new UpdateProfileCommand(null, null, "  санкт   петербург "), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Санкт Петербург", user.Profile.City);
    }

    [Fact]
    public async Task Handle_ValidProfile_TrimsAndUpdatesFields()
    {
        var userId = UserId.New();
        _currentUser.Id.Returns(userId);
        var user = User.Create("someone", Email.Create("someone@test.com").Value!, "hash");
        _users.GetByIdAsync(Arg.Any<UserId>()).Returns(user);
        var handler = CreateHandler();

        var result = await handler.Handle(new UpdateProfileCommand("  Grog  ", "  Barbarian  ", "Москва"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Grog", user.Profile.DisplayName);
        Assert.Equal("Barbarian", user.Profile.Bio);
        _users.Received(1).Update(user);
    }
}
