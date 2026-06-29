using TTRPGHub.Common;
using TTRPGHub.Entities;

namespace TTRPGHub.Events;

public sealed record UserCreatedEvent(UserId UserId) : IDomainEvent;
