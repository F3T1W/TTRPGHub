using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.Users.Queries.GetAllUsers;

internal sealed class GetAllUsersQueryHandler(IUserRepository users)
    : IRequestHandler<GetAllUsersQuery, Result<AdminUserPageDto>>
{
    public async Task<Result<AdminUserPageDto>> Handle(GetAllUsersQuery request, CancellationToken ct)
    {
        var (items, total) = await users.SearchAsync(request.Search, request.Page, request.PageSize, ct);

        var dtos = items
            .Select(u => new AdminUserDto(u.Id.Value, u.Username, u.Email.Value, u.Role.ToString(), u.CreatedAt))
            .ToList();

        return new AdminUserPageDto(dtos, total, request.Page, request.PageSize);
    }
}
