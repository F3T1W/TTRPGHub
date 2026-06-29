using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Entities;

namespace TTRPGHub.Features.Initiative.Commands.SetEntries;

public sealed record SetEntriesCommand(Guid TrackerId, List<EntryInput> Entries) : IRequest<Result>;

public sealed record EntryInput(
    string Name, int Initiative, int MaxHp, int CurrentHp,
    int ArmorClass, bool IsPlayerCharacter, string? Notes);
