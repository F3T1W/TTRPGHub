using TTRPGHub.Entities;

namespace TTRPGHub.Common.Interfaces;

public interface ICurrentUser
{
    UserId Id { get; }
    bool IsAuthenticated { get; }
}
