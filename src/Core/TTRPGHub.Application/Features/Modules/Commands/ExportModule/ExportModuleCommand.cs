using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.Modules.Commands.ExportModule;

// J.9 — "модуль" здесь не произвольный исполняемый код (это уже есть и намеренно ограничено
// песочницей — см. K.7), а переносимый контент-пак: набор макросов + записи своего кастомного
// справочника (RuleEntry), упакованные в один JSON-файл с метаданными автора/версии. GM
// публикует его как файл (Discord/форум/файлообменник — своей площадки-маркетплейса нет),
// другой GM импортирует одним файлом вместо ручного пересоздания каждого макроса/правила.
public sealed record ExportModuleCommand(
    string Name, string? Description, string? Version,
    List<Guid> MacroIds, string? SystemSlug) : IRequest<Result<string>>;
