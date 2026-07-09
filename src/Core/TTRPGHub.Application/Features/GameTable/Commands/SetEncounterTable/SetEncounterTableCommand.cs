using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.GameTable.Commands.SetEncounterTable;

public sealed record SetEncounterTableCommand(Guid SessionId, string? EncounterTableJson) : IRequest<Result>;
