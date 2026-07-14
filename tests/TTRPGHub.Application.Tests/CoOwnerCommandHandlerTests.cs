using NSubstitute;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Features.Characters.Commands.AddCoOwner;
using TTRPGHub.Features.Characters.Commands.RemoveCoOwner;
using TTRPGHub.Repositories;
using TTRPGHub.ValueObjects;

namespace TTRPGHub.Application.Tests;

public class AddCoOwnerCommandHandlerTests
{
    private readonly ICharacterRepository _characterRepository = Substitute.For<ICharacterRepository>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICacheService _cache = Substitute.For<ICacheService>();

    private AddCoOwnerCommandHandler CreateHandler() =>
        new(_characterRepository, _userRepository, _currentUser, _unitOfWork, _cache);

    private static User CreateUser(string username) =>
        User.Create(username, Email.Create($"{username.ToLowerInvariant()}@example.com").Value!, "hash");

    [Fact]
    public async Task Handle_CharacterNotFound_ReturnsNotFound()
    {
        _characterRepository.GetByIdAsync(Arg.Any<CharacterId>()).Returns((Character?)null);
        var handler = CreateHandler();

        var result = await handler.Handle(new AddCoOwnerCommand(Guid.NewGuid(), "someone"), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_NotOwner_ReturnsUnauthorized()
    {
        var character = Character.Create(UserId.New(), "Aragorn", "Human", "Fighter", 1).Value!;
        _characterRepository.GetByIdAsync(Arg.Any<CharacterId>()).Returns(character);
        _currentUser.Id.Returns(UserId.New());
        var handler = CreateHandler();

        var result = await handler.Handle(new AddCoOwnerCommand(Guid.NewGuid(), "someone"), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_UsernameNotFound_ReturnsNotFound()
    {
        var ownerId = UserId.New();
        var character = Character.Create(ownerId, "Aragorn", "Human", "Fighter", 1).Value!;
        _characterRepository.GetByIdAsync(Arg.Any<CharacterId>()).Returns(character);
        _currentUser.Id.Returns(ownerId);
        _userRepository.SearchAsync("ghost", 1, 5).Returns(((IReadOnlyList<User>)[], 0));
        var handler = CreateHandler();

        var result = await handler.Handle(new AddCoOwnerCommand(Guid.NewGuid(), "ghost"), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_ValidTarget_AddsCoOwner()
    {
        var ownerId = UserId.New();
        var character = Character.Create(ownerId, "Aragorn", "Human", "Fighter", 1).Value!;
        _characterRepository.GetByIdAsync(Arg.Any<CharacterId>()).Returns(character);
        _currentUser.Id.Returns(ownerId);
        var target = CreateUser("Legolas");
        _userRepository.SearchAsync("Legolas", 1, 5).Returns(((IReadOnlyList<User>)[target], 1));
        var handler = CreateHandler();

        var result = await handler.Handle(new AddCoOwnerCommand(Guid.NewGuid(), "Legolas"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(character.IsOwnedBy(target.Id));
    }
}

public class RemoveCoOwnerCommandHandlerTests
{
    private readonly ICharacterRepository _characterRepository = Substitute.For<ICharacterRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICacheService _cache = Substitute.For<ICacheService>();

    private RemoveCoOwnerCommandHandler CreateHandler() =>
        new(_characterRepository, _currentUser, _unitOfWork, _cache);

    [Fact]
    public async Task Handle_NotOwner_ReturnsUnauthorized()
    {
        var character = Character.Create(UserId.New(), "Aragorn", "Human", "Fighter", 1).Value!;
        _characterRepository.GetByIdAsync(Arg.Any<CharacterId>()).Returns(character);
        _currentUser.Id.Returns(UserId.New());
        var handler = CreateHandler();

        var result = await handler.Handle(new RemoveCoOwnerCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_Owner_RemovesCoOwner()
    {
        var ownerId = UserId.New();
        var character = Character.Create(ownerId, "Aragorn", "Human", "Fighter", 1).Value!;
        var coOwnerId = Guid.NewGuid();
        character.AddCoOwner(coOwnerId);
        _characterRepository.GetByIdAsync(Arg.Any<CharacterId>()).Returns(character);
        _currentUser.Id.Returns(ownerId);
        var handler = CreateHandler();

        var result = await handler.Handle(new RemoveCoOwnerCommand(Guid.NewGuid(), coOwnerId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(character.IsOwnedBy(new UserId(coOwnerId)));
    }
}
