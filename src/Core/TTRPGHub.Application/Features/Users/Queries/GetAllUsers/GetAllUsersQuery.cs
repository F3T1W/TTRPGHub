using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.Users.Queries.GetAllUsers;

public sealed record GetAllUsersQuery(string? Search = null, int Page = 1, int PageSize = 30)
    : IRequest<Result<AdminUserPageDto>>;

public sealed record AdminUserDto(Guid Id, string Username, string Email, string Role, DateTime CreatedAt);

public sealed record AdminUserPageDto(List<AdminUserDto> Items, int Total, int Page, int PageSize)
{
    public int TotalPages => Math.Max(1, (int)Math.Ceiling((double)Total / PageSize));
}
