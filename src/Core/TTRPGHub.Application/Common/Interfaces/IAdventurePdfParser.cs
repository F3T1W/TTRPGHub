namespace TTRPGHub.Common.Interfaces;

// M.1 — импорт купленного приключения (PDF) в приватный контент своей сессии, а не в общий
// справочник (см. ROADMAP): парсинг вынесен за интерфейс, чтобы Application не тянул
// PDF-библиотеку напрямую (та же причина, что у IStorageService для MinIO).
public interface IAdventurePdfParser
{
    IReadOnlyList<AdventurePdfPage> ParsePages(Stream pdfStream);
}

public sealed record AdventurePdfPage(int PageNumber, string Text, IReadOnlyList<AdventurePdfImage> Images);

public sealed record AdventurePdfImage(byte[] Bytes, string ContentType, int Width, int Height);
