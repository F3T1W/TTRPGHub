using NSubstitute;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Features.Macros.Commands.SetMacroHotbarSlot;
using TTRPGHub.Repositories;

namespace TTRPGHub.Application.Tests;

public class SetMacroHotbarSlotCommandHandlerTests
{
    private readonly IMacroRepository _macroRepository = Substitute.For<IMacroRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    private SetMacroHotbarSlotCommandHandler CreateHandler() =>
        new(_macroRepository, _unitOfWork, _currentUser);

    [Theory]
    [InlineData(-2)]
    [InlineData(30)]
    public async Task Handle_SlotOutOfRange_ReturnsValidationError(int slot)
    {
        var handler = CreateHandler();

        var result = await handler.Handle(new SetMacroHotbarSlotCommand(Guid.NewGuid(), slot), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_MacroNotOwnedByCaller_ReturnsUnauthorized()
    {
        var macro = Macro.Create(UserId.New(), "Heal", null, MacroType.Chat, "/r 1d8");
        _macroRepository.GetByIdAsync(Arg.Any<Guid>()).Returns(macro);
        _currentUser.Id.Returns(UserId.New());
        var handler = CreateHandler();

        var result = await handler.Handle(new SetMacroHotbarSlotCommand(macro.Id, 0), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_SlotAlreadyOccupiedByAnotherMacro_ClearsOldSlot()
    {
        var ownerId = UserId.New();
        var macro = Macro.Create(ownerId, "Heal", null, MacroType.Chat, "/r 1d8");
        var occupying = Macro.Create(ownerId, "Damage", null, MacroType.Chat, "/r 1d6");
        occupying.SetHotbarSlot(3);
        _macroRepository.GetByIdAsync(macro.Id).Returns(macro);
        _macroRepository.GetByOwnerAsync(ownerId).Returns([macro, occupying]);
        _currentUser.Id.Returns(ownerId);
        var handler = CreateHandler();

        var result = await handler.Handle(new SetMacroHotbarSlotCommand(macro.Id, 3), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(-1, occupying.HotbarSlot);
        Assert.Equal(3, macro.HotbarSlot);
    }

    [Fact]
    public async Task Handle_SlotWithinExtendedPageRange_Succeeds()
    {
        var ownerId = UserId.New();
        var macro = Macro.Create(ownerId, "Heal", null, MacroType.Chat, "/r 1d8");
        _macroRepository.GetByIdAsync(macro.Id).Returns(macro);
        _macroRepository.GetByOwnerAsync(ownerId).Returns([macro]);
        _currentUser.Id.Returns(ownerId);
        var handler = CreateHandler();

        var result = await handler.Handle(new SetMacroHotbarSlotCommand(macro.Id, 25), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(25, macro.HotbarSlot);
    }
}
