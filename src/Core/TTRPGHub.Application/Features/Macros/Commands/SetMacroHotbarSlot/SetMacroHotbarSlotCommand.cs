using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.Macros.Commands.SetMacroHotbarSlot;

// Slot: -1 снимает макрос с хотбара, 0-9 назначает его на конкретный слот. Если слот уже занят
// другим макросом — сервер сам снимает старый (в хотбаре не может быть двух макросов в одном
// слоте), клиенту не нужно самому чистить конфликт.
public sealed record SetMacroHotbarSlotCommand(Guid MacroId, int Slot) : IRequest<Result>;
