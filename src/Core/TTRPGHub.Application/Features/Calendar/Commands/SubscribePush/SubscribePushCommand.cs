using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.Calendar.Commands.SubscribePush;

public sealed record SubscribePushCommand(string Endpoint, string P256dh, string Auth) : IRequest<Result>;
