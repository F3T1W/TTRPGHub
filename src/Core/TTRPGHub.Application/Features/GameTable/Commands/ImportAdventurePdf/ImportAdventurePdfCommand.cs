using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.GameTable.Commands.ImportAdventurePdf;

// M.1 — импорт купленного приключения (PDF) в приватный контент сессии: текст по страницам
// уходит в Journal (папка + дочерние записи, видны только этой сессии — см. ROADMAP про
// разграничение "ГМ показывает партии купленное" vs "платформа раздаёт чужой контент всем"),
// извлечённые из PDF картинки-карты не становятся сценами автоматически — только загружаются
// в хранилище и возвращаются GM на выбор (ImportedMapImage.Url), чтобы не заспамить список
// сцен повторяющимися декоративными иллюстрациями без разбора.
public sealed record ImportAdventurePdfCommand(
    Guid SessionId, Stream FileStream, string FileName, long FileSize
) : IRequest<Result<ImportAdventurePdfResponse>>;

public sealed record ImportAdventurePdfResponse(
    Guid FolderEntryId, int PagesImported, List<ImportedMapImage> Images);

public sealed record ImportedMapImage(int PageNumber, string Url, int Width, int Height);
