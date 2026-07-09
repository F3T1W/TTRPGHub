using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.GameTable.Commands.ImportAdventurePdf;

internal sealed class ImportAdventurePdfCommandHandler(
    IGameSessionRepository sessionRepository,
    IJournalEntryRepository journalRepository,
    IAdventurePdfParser parser,
    IStorageService storage,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser
) : IRequestHandler<ImportAdventurePdfCommand, Result<ImportAdventurePdfResponse>>
{
    private const string Bucket = "adventure-imports";
    private const long MaxBytes = 80 * 1024 * 1024; // 80 MB — покрывает типичный иллюстрированный PDF-модуль
    private const int MaxImages = 20; // предохранитель от спама картинками на нетипично богатом иллюстрациями файле
    private const int MinTextLength = 30; // короче — титульная/пустая страница, отдельная запись Journal не нужна

    public async Task<Result<ImportAdventurePdfResponse>> Handle(ImportAdventurePdfCommand command, CancellationToken ct)
    {
        if (!command.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            return Error.Validation("AdventureImport.Format", "Ожидается файл в формате .pdf.");
        if (command.FileSize > MaxBytes)
            return Error.Validation("AdventureImport.Size", "Максимальный размер файла — 80 МБ.");

        var session = await sessionRepository.GetByIdAsync(new GameSessionId(command.SessionId), ct);
        if (session is null)
            return Error.NotFound(nameof(GameSession));

        // Импортирует только ГМ — это его купленный контент для его собственной партии,
        // не общий справочник платформы (см. комментарий в ImportAdventurePdfCommand.cs).
        if (session.OrganizerId != currentUser.Id)
            return Error.Unauthorized();

        List<AdventurePdfPage> pages;
        try { pages = [.. parser.ParsePages(command.FileStream)]; }
        catch { return Error.Validation("AdventureImport.Parse", "Не удалось разобрать PDF — файл повреждён или защищён паролем."); }

        var folderTitle = command.FileName[..^4].Trim();
        var folder = JournalEntry.Create(session.Id, currentUser.Id, folderTitle, "");
        await journalRepository.AddAsync(folder, ct);

        var pagesImported = 0;
        foreach (var page in pages)
        {
            if (page.Text.Trim().Length < MinTextLength)
                continue;

            var pageEntry = JournalEntry.Create(
                session.Id, currentUser.Id, $"Стр. {page.PageNumber}", page.Text, folder.Id);
            await journalRepository.AddAsync(pageEntry, ct);
            pagesImported++;
        }

        await unitOfWork.SaveChangesAsync(ct);

        var images = new List<ImportedMapImage>();
        if (pages.Any(p => p.Images.Count > 0))
            await storage.EnsureBucketExistsAsync(Bucket, ct);

        foreach (var page in pages)
        {
            foreach (var image in page.Images)
            {
                if (images.Count >= MaxImages) break;

                var ext = image.ContentType == "image/png" ? "png" : "jpg";
                var objectName = $"{command.SessionId}/{Guid.NewGuid():N}.{ext}";
                using var imgStream = new MemoryStream(image.Bytes);
                var url = await storage.UploadAsync(Bucket, objectName, imgStream, image.ContentType, ct);
                images.Add(new ImportedMapImage(page.PageNumber, url, image.Width, image.Height));
            }
            if (images.Count >= MaxImages) break;
        }

        return new ImportAdventurePdfResponse(folder.Id, pagesImported, images);
    }
}
