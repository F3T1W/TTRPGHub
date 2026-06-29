using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.Campaigns.Commands.RemoveParticipant;

public sealed record RemoveParticipantCommand(Guid CampaignId, Guid UserId) : IRequest<Result>;
