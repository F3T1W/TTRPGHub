using TTRPGHub.Domain.Common;
using TTRPGHub.Domain.Entities;

namespace TTRPGHub.Domain.Events;

public sealed record UserCreatedEvent(UserId UserId) : IDomainEvent;
