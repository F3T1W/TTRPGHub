using TTRPGHub.Common;
using TTRPGHub.Entities;

namespace TTRPGHub.Events;

public sealed record CampaignCreatedEvent(CampaignId CampaignId, UserId OrganizerId) : IDomainEvent;
