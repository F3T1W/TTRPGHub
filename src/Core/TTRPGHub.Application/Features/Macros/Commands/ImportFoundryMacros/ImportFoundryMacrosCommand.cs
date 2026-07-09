using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Features.Macros.Shared;

namespace TTRPGHub.Features.Macros.Commands.ImportFoundryMacros;

// FileContent — сырой текст экспортированного Foundry-файла: либо один макрос ({name,type,img,
// command}), либо массив макросов (обычный экспорт папки Macro Directory / world pack). Парсинг
// в обработчике — структура переносится 1:1 (имя/иконка/тип/текст команды), сам JS-код команды
// не адаптируется автоматически (Foundry API у нас нет, см. Macro.cs) — пользователь правит
// команду сам после импорта, ориентируясь на нашу песочницу game.*.
public sealed record ImportFoundryMacrosCommand(string FileContent) : IRequest<Result<List<MacroDto>>>;
