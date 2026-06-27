using TTRPGHub.Domain.Entities;

namespace TTRPGHub.Application.Common.Interfaces;

public interface ICurrentUser
{
    UserId Id { get; }
    string Username { get; }
    bool IsAuthenticated { get; }
}
