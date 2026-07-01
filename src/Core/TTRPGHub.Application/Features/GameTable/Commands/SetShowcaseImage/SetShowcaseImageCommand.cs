using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.GameTable.Commands.SetShowcaseImage;

public sealed record SetShowcaseImageCommand(Guid SessionId, string? ImageUrl) : IRequest<Result>;
