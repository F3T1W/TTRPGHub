using NSubstitute;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Features.Initiative.Shared;
using TTRPGHub.Repositories;

namespace TTRPGHub.Application.Tests;

public class InitiativeTrackerSyncTests
{
    private readonly ITableTokenRepository _tokenRepository = Substitute.For<ITableTokenRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ITableNotifier _tableNotifier = Substitute.For<ITableNotifier>();

    private InitiativeTrackerSync CreateSync() => TestDoubles.CreateInertTrackerSync(
        tokenRepository: _tokenRepository, unitOfWork: _unitOfWork, tableNotifier: _tableNotifier);

    private static InitiativeEntry CreateEntry(Guid linkedTokenId, EntryStatus status = EntryStatus.Active) => new()
    {
        Name = "Goblin", CurrentHp = 5, LinkedTokenId = linkedTokenId, Status = status,
    };

    [Fact]
    public async Task PushEntryToTokenAsync_NoLinkedToken_DoesNothing()
    {
        var entry = new InitiativeEntry { Name = "Goblin", LinkedTokenId = null };
        var sync = CreateSync();

        await sync.PushEntryToTokenAsync(entry, CancellationToken.None);

        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PushEntryToTokenAsync_TokenNotFound_DoesNothing()
    {
        var entry = CreateEntry(Guid.NewGuid());
        _tokenRepository.GetByIdAsync(Arg.Any<Guid>()).Returns((TableToken?)null);
        var sync = CreateSync();

        await sync.PushEntryToTokenAsync(entry, CancellationToken.None);

        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PushEntryToTokenAsync_UpdatesTokenHp()
    {
        var token = TableToken.Create(GameSessionId.New(), Guid.NewGuid(), "Goblin", null, "#f00", 1, 1, null,
            currentHp: 10, maxHp: 10, armorClass: 12);
        var entry = CreateEntry(token.Id);
        entry.CurrentHp = 3;
        _tokenRepository.GetByIdAsync(token.Id).Returns(token);
        var sync = CreateSync();

        await sync.PushEntryToTokenAsync(entry, CancellationToken.None);

        Assert.Equal(3, token.CurrentHp);
        await _tableNotifier.Received(1).NotifyTokenUpdatedAsync(
            token.SessionId.Value, Arg.Any<Features.GameTable.Shared.TableTokenDto>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PushEntryToTokenAsync_DeadStatus_AppliesDeadCondition()
    {
        var token = TableToken.Create(GameSessionId.New(), Guid.NewGuid(), "Goblin", null, "#f00", 1, 1, null,
            currentHp: 0, maxHp: 10, armorClass: 12);
        var entry = CreateEntry(token.Id, EntryStatus.Dead);
        entry.CurrentHp = 0;
        _tokenRepository.GetByIdAsync(token.Id).Returns(token);
        var sync = CreateSync();

        await sync.PushEntryToTokenAsync(entry, CancellationToken.None);

        Assert.Contains(token.Conditions, c => c.Slug == "dead");
    }

    [Fact]
    public async Task PushEntryToTokenAsync_NoLongerDead_RemovesDeadCondition()
    {
        var token = TableToken.Create(GameSessionId.New(), Guid.NewGuid(), "Goblin", null, "#f00", 1, 1, null,
            currentHp: 0, maxHp: 10, armorClass: 12);
        token.ApplyCondition("dead", "Dead", null);
        var entry = CreateEntry(token.Id, EntryStatus.Unconscious);
        entry.CurrentHp = 1;
        _tokenRepository.GetByIdAsync(token.Id).Returns(token);
        var sync = CreateSync();

        await sync.PushEntryToTokenAsync(entry, CancellationToken.None);

        Assert.DoesNotContain(token.Conditions, c => c.Slug == "dead");
    }

    [Fact]
    public async Task PushEntryToTokenAsync_ActiveStatus_DoesNotTouchUnrelatedConditions()
    {
        var token = TableToken.Create(GameSessionId.New(), Guid.NewGuid(), "Goblin", null, "#f00", 1, 1, null,
            currentHp: 5, maxHp: 10, armorClass: 12);
        token.ApplyCondition("frightened", "Frightened", 1);
        var entry = CreateEntry(token.Id, EntryStatus.Active);
        entry.CurrentHp = 5;
        _tokenRepository.GetByIdAsync(token.Id).Returns(token);
        var sync = CreateSync();

        await sync.PushEntryToTokenAsync(entry, CancellationToken.None);

        Assert.Contains(token.Conditions, c => c.Slug == "frightened");
    }
}
