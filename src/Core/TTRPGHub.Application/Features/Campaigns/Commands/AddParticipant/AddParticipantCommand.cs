using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.Campaigns.Commands.AddParticipant;

public sealed record AddParticipantCommand(Guid CampaignId, Guid UserId) : IRequest<Result>;
