using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Features.GameTable.Shared;

namespace TTRPGHub.Features.GameTable.Commands.RollEncounterTable;

public sealed record RollEncounterTableCommand(Guid SessionId) : IRequest<Result<TableMessageDto>>;
