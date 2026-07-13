using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.Users.Commands.UpdateProfile;

public sealed record UpdateProfileCommand(string? DisplayName, string? Bio, string? City) : IRequest<Result>;
