using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.Calendar.Commands.UnsubscribePush;

public sealed record UnsubscribePushCommand(string Endpoint) : IRequest<Result>;
