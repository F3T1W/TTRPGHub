namespace TTRPGHub.Common.Interfaces;

public interface ITranslationService
{
    Task<string> TranslateAsync(string text, CancellationToken ct = default);
}
