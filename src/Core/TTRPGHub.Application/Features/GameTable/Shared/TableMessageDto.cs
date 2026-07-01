using TTRPGHub.Entities;

namespace TTRPGHub.Features.GameTable.Shared;

public sealed record TableMessageDto(
    Guid Id, Guid SenderId, string SenderUsername,
    Guid? RecipientId, string? RecipientUsername,
    TableMessageKind Kind, string Content, DateTime CreatedAt);
