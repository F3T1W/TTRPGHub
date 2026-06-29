using TTRPGHub.Common;
using TTRPGHub.Entities;

namespace TTRPGHub.Events;

public sealed record GameSessionCreatedEvent(GameSessionId SessionId, UserId OrganizerId) : IDomainEvent;
