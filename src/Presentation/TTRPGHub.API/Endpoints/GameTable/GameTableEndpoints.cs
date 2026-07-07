using MediatR;
using TTRPGHub.Extensions;
using TTRPGHub.Features.GameTable.Commands.AddTableToken;
using TTRPGHub.Features.GameTable.Commands.AdvanceTurn;
using TTRPGHub.Features.GameTable.Commands.StartCombat;
using TTRPGHub.Features.GameTable.Commands.EndCombat;
using TTRPGHub.Features.GameTable.Commands.CreateScene;
using TTRPGHub.Features.GameTable.Commands.RenameScene;
using TTRPGHub.Features.GameTable.Commands.DeleteScene;
using TTRPGHub.Features.GameTable.Commands.ActivateScene;
using TTRPGHub.Features.GameTable.Commands.ApplyTokenCondition;
using TTRPGHub.Features.GameTable.Commands.RemoveTokenCondition;
using TTRPGHub.Features.GameTable.Commands.ClearTableAudio;
using TTRPGHub.Features.GameTable.Commands.CreateJournalEntry;
using TTRPGHub.Features.GameTable.Commands.DeleteJournalEntry;
using TTRPGHub.Features.GameTable.Commands.SetJournalEntryPublished;
using TTRPGHub.Features.GameTable.Commands.SetJournalEntryVisibility;
using TTRPGHub.Features.GameTable.Commands.UpdateJournalEntry;
using TTRPGHub.Features.GameTable.Queries.GetJournalEntries;
using TTRPGHub.Features.GameTable.Commands.MoveTableToken;
using TTRPGHub.Features.GameTable.Commands.PauseTableAudio;
using TTRPGHub.Features.GameTable.Commands.PlayTableAudio;
using TTRPGHub.Features.GameTable.Commands.RemoveTableToken;
using TTRPGHub.Features.GameTable.Commands.RollDice;
using TTRPGHub.Features.GameTable.Commands.SeekTableAudio;
using TTRPGHub.Features.GameTable.Commands.SendChatMessage;
using TTRPGHub.Features.GameTable.Commands.SendWhisper;
using TTRPGHub.Features.GameTable.Commands.SetFogSettings;
using TTRPGHub.Features.GameTable.Commands.SetSceneEnvironment;
using TTRPGHub.Features.GameTable.Commands.SetWalls;
using TTRPGHub.Features.GameTable.Commands.SetLights;
using TTRPGHub.Features.GameTable.Commands.SetGridCellSize;
using TTRPGHub.Features.GameTable.Commands.SetShowcaseImage;
using TTRPGHub.Features.GameTable.Commands.SetTableTrack;
using TTRPGHub.Features.GameTable.Commands.SetTokenVisibility;
using TTRPGHub.Features.GameTable.Commands.UpdateTokenStats;
using TTRPGHub.Features.GameTable.Commands.UploadShowcaseImage;
using TTRPGHub.Features.GameTable.Commands.UploadTableTrack;
using TTRPGHub.Features.GameTable.Commands.UploadTokenImage;
using TTRPGHub.Features.GameTable.Queries.GetSessionCharacters;
using TTRPGHub.Features.GameTable.Queries.GetTableState;

namespace TTRPGHub.Endpoints.GameTable;

internal static class GameTableEndpoints
{
    internal static void MapGameTableEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/table").WithTags("GameTable").RequireAuthorization();

        group.MapGet("/{sessionId:guid}/state", async (Guid sessionId, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetTableStateQuery(sessionId), ct);
            return result.ToResponse();
        })
        .WithSummary("Состояние игрового стола")
        .Produces<TableStateDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/{sessionId:guid}/messages", async (Guid sessionId, SendChatRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new SendChatMessageCommand(sessionId, request.Content), ct);
            return result.ToResponse();
        })
        .WithSummary("Отправить сообщение в чат стола");

        group.MapPost("/{sessionId:guid}/roll", async (Guid sessionId, RollDiceRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new RollDiceCommand(sessionId, request.Expression, request.Dc, request.Label), ct);
            return result.ToResponse();
        })
        .WithSummary("Бросить кубики (со Сложностью — вернёт степень успеха PF2e)");

        group.MapPost("/{sessionId:guid}/whisper", async (Guid sessionId, SendWhisperRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new SendWhisperCommand(sessionId, request.RecipientUserId, request.Content), ct);
            return result.ToResponse();
        })
        .WithSummary("Личное сообщение от ГМа игроку");

        group.MapPut("/{sessionId:guid}/showcase", async (Guid sessionId, SetShowcaseRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new SetShowcaseImageCommand(sessionId, request.ImageUrl), ct);
            return result.ToResponse();
        })
        .WithSummary("Установить showcase-изображение (только ГМ)");

        group.MapPost("/{sessionId:guid}/showcase/upload", async (Guid sessionId, IFormFile file, ISender sender, CancellationToken ct) =>
        {
            var command = new UploadShowcaseImageCommand(sessionId, file.OpenReadStream(), file.ContentType, file.Length);
            var result = await sender.Send(command, ct);
            return result.IsSuccess
                ? Results.Ok(new { url = result.Value })
                : result.ToResponse();
        })
        .WithSummary("Загрузить showcase-изображение (только ГМ)")
        .DisableAntiforgery();

        group.MapPut("/{sessionId:guid}/audio/track", async (Guid sessionId, SetTrackRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new SetTableTrackCommand(sessionId, request.TrackUrl, request.TrackTitle), ct);
            return result.ToResponse();
        })
        .WithSummary("Установить трек по URL (только ГМ)");

        group.MapPost("/{sessionId:guid}/audio/upload", async (Guid sessionId, IFormFile file, ISender sender, CancellationToken ct) =>
        {
            var command = new UploadTableTrackCommand(sessionId, file.OpenReadStream(), file.FileName, file.ContentType, file.Length);
            var result = await sender.Send(command, ct);
            return result.IsSuccess
                ? Results.Ok(new { url = result.Value })
                : result.ToResponse();
        })
        .WithSummary("Загрузить аудиофайл (только ГМ)")
        .DisableAntiforgery();

        group.MapPost("/{sessionId:guid}/audio/play", async (Guid sessionId, AudioPositionRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new PlayTableAudioCommand(sessionId, request.PositionSeconds), ct);
            return result.ToResponse();
        })
        .WithSummary("Запустить воспроизведение (только ГМ)");

        group.MapPost("/{sessionId:guid}/audio/pause", async (Guid sessionId, AudioPositionRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new PauseTableAudioCommand(sessionId, request.PositionSeconds), ct);
            return result.ToResponse();
        })
        .WithSummary("Поставить на паузу (только ГМ)");

        group.MapPost("/{sessionId:guid}/audio/seek", async (Guid sessionId, AudioPositionRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new SeekTableAudioCommand(sessionId, request.PositionSeconds), ct);
            return result.ToResponse();
        })
        .WithSummary("Перемотать (только ГМ)");

        group.MapDelete("/{sessionId:guid}/audio", async (Guid sessionId, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new ClearTableAudioCommand(sessionId), ct);
            return result.ToResponse();
        })
        .WithSummary("Убрать трек (только ГМ)");

        group.MapPost("/{sessionId:guid}/tokens", async (Guid sessionId, AddTokenRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new AddTableTokenCommand(
                sessionId, request.Label, request.ImageUrl, request.Color,
                request.X, request.Y, request.OwnerUserId,
                request.Width, request.Height, request.CombatantType, request.CombatantId), ct);
            return result.ToResponse();
        })
        .WithSummary("Добавить жетон на карту (только ГМ) — можно привязать к персонажу/монстру");

        group.MapPut("/{sessionId:guid}/tokens/{tokenId:guid}/position", async (Guid sessionId, Guid tokenId, TokenPositionRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new MoveTableTokenCommand(sessionId, tokenId, request.X, request.Y), ct);
            return result.ToResponse();
        })
        .WithSummary("Переместить жетон");

        group.MapPatch("/{sessionId:guid}/tokens/{tokenId:guid}/stats", async (Guid sessionId, Guid tokenId, UpdateTokenStatsRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new UpdateTokenStatsCommand(
                sessionId, tokenId, request.CurrentHp, request.Width, request.Height, request.Rotation,
                request.SetInitiative, request.Initiative, request.HasDarkvision, request.HasLowLightVision), ct);
            return result.ToResponse();
        })
        .WithSummary("Изменить HP/размер/поворот/инициативу/тёмное зрение жетона");

        group.MapPut("/{sessionId:guid}/tokens/{tokenId:guid}/visibility", async (Guid sessionId, Guid tokenId, SetTokenVisibilityRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new SetTokenVisibilityCommand(sessionId, tokenId, request.VisibleToUserIds), ct);
            return result.ToResponse();
        })
        .WithSummary("Ограничить видимость жетона (только ГМ): null — видят все, [] — скрыт от игроков, список — видят только эти игроки");

        group.MapPost("/{sessionId:guid}/tokens/{tokenId:guid}/image", async (Guid sessionId, Guid tokenId, IFormFile file, ISender sender, CancellationToken ct) =>
        {
            var command = new UploadTokenImageCommand(sessionId, tokenId, file.OpenReadStream(), file.FileName, file.ContentType, file.Length);
            var result = await sender.Send(command, ct);
            return result.IsSuccess
                ? Results.Ok(new { url = result.Value })
                : result.ToResponse();
        })
        .WithSummary("Загрузить свою картинку на жетон")
        .DisableAntiforgery();

        group.MapDelete("/{sessionId:guid}/tokens/{tokenId:guid}", async (Guid sessionId, Guid tokenId, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new RemoveTableTokenCommand(sessionId, tokenId), ct);
            return result.ToResponse();
        })
        .WithSummary("Удалить жетон (только ГМ)");

        group.MapPut("/{sessionId:guid}/grid", async (Guid sessionId, SetGridCellSizeRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new SetGridCellSizeCommand(sessionId, request.Px), ct);
            return result.ToResponse();
        })
        .WithSummary("Установить размер клетки сетки в px (только ГМ)");

        group.MapPut("/{sessionId:guid}/fog", async (Guid sessionId, SetFogSettingsRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new SetFogSettingsCommand(sessionId, request.Enabled, request.VisionRadiusFeet), ct);
            return result.ToResponse();
        })
        .WithSummary("Включить/выключить туман войны и задать радиус зрения (только ГМ)");

        group.MapPut("/{sessionId:guid}/environment", async (Guid sessionId, SetSceneEnvironmentRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new SetSceneEnvironmentCommand(sessionId, request.TerrainTagsJson, request.AmbientLighting), ct);
            return result.ToResponse();
        })
        .WithSummary("Местность и освещение сцены для предикатов PF2e (только ГМ)");

        group.MapPut("/{sessionId:guid}/walls", async (Guid sessionId, SetWallsRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new SetWallsCommand(sessionId, request.WallsJson), ct);
            return result.ToResponse();
        })
        .WithSummary("Сохранить список стен сцены для line-of-sight тумана (только ГМ)");

        group.MapPut("/{sessionId:guid}/lights", async (Guid sessionId, SetLightsRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new SetLightsCommand(sessionId, request.LightsJson), ct);
            return result.ToResponse();
        })
        .WithSummary("Сохранить список источников света сцены (только ГМ)");

        group.MapPost("/{sessionId:guid}/combat/start", async (Guid sessionId, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new StartCombatCommand(sessionId), ct);
            return result.ToResponse();
        })
        .WithSummary("Начать бой — трекер инициативы (только ГМ)");

        group.MapPost("/{sessionId:guid}/combat/end", async (Guid sessionId, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new EndCombatCommand(sessionId), ct);
            return result.ToResponse();
        })
        .WithSummary("Завершить бой (только ГМ)");

        group.MapPost("/{sessionId:guid}/combat/next", async (Guid sessionId, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new AdvanceTurnCommand(sessionId, Forward: true), ct);
            return result.ToResponse();
        })
        .WithSummary("Следующий ход (только ГМ)");

        group.MapPost("/{sessionId:guid}/combat/previous", async (Guid sessionId, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new AdvanceTurnCommand(sessionId, Forward: false), ct);
            return result.ToResponse();
        })
        .WithSummary("Предыдущий ход (только ГМ)");

        group.MapPost("/{sessionId:guid}/scenes", async (Guid sessionId, CreateSceneRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new CreateSceneCommand(sessionId, request.Name), ct);
            return result.IsSuccess
                ? Results.Created($"/api/table/{sessionId}/scenes/{result.Value!.Id}", result.Value)
                : result.ToResponse();
        })
        .WithSummary("Создать новую сцену (карту) в рамках сессии (только ГМ)");

        group.MapPut("/{sessionId:guid}/scenes/{sceneId:guid}", async (Guid sessionId, Guid sceneId, CreateSceneRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new RenameSceneCommand(sessionId, sceneId, request.Name), ct);
            return result.ToResponse();
        })
        .WithSummary("Переименовать сцену (только ГМ)");

        group.MapDelete("/{sessionId:guid}/scenes/{sceneId:guid}", async (Guid sessionId, Guid sceneId, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new DeleteSceneCommand(sessionId, sceneId), ct);
            return result.ToResponse();
        })
        .WithSummary("Удалить сцену вместе с её токенами (только ГМ, нельзя удалить последнюю)");

        group.MapPost("/{sessionId:guid}/scenes/{sceneId:guid}/activate", async (Guid sessionId, Guid sceneId, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new ActivateSceneCommand(sessionId, sceneId), ct);
            return result.ToResponse();
        })
        .WithSummary("Переключить активную сцену сессии (только ГМ)");

        group.MapGet("/{sessionId:guid}/characters", async (Guid sessionId, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetSessionCharactersQuery(sessionId), ct);
            return result.ToResponse();
        })
        .WithSummary("Персонажи участников сессии (только ГМ, для привязки к жетону)");

        group.MapPost("/{sessionId:guid}/tokens/{tokenId:guid}/conditions", async (Guid sessionId, Guid tokenId, ApplyConditionRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new ApplyTokenConditionCommand(sessionId, tokenId, request.Slug, request.Name, request.Value), ct);
            return result.ToResponse();
        })
        .WithSummary("Наложить состояние на жетон");

        group.MapDelete("/{sessionId:guid}/tokens/{tokenId:guid}/conditions/{slug}", async (Guid sessionId, Guid tokenId, string slug, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new RemoveTokenConditionCommand(sessionId, tokenId, slug), ct);
            return result.ToResponse();
        })
        .WithSummary("Снять состояние с жетона");

        group.MapGet("/{sessionId:guid}/journal", async (Guid sessionId, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetJournalEntriesQuery(sessionId), ct);
            return result.ToResponse();
        })
        .WithSummary("Записи журнала мастера (игрокам видны только опубликованные)");

        group.MapPost("/{sessionId:guid}/journal", async (Guid sessionId, CreateJournalEntryRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new CreateJournalEntryCommand(
                sessionId, request.Title, request.ContentMarkdown, request.ParentId, request.CampaignId), ct);
            return result.IsSuccess ? Results.Created($"/api/table/{sessionId}/journal/{result.Value!.Id}", result.Value) : result.ToResponse();
        })
        .WithSummary("Создать запись журнала (черновик, только ГМ)");

        group.MapPut("/{sessionId:guid}/journal/{entryId:guid}", async (Guid sessionId, Guid entryId, CreateJournalEntryRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new UpdateJournalEntryCommand(
                sessionId, entryId, request.Title, request.ContentMarkdown, request.ParentId, request.CampaignId), ct);
            return result.ToResponse();
        })
        .WithSummary("Обновить запись журнала (только ГМ)");

        group.MapPut("/{sessionId:guid}/journal/{entryId:guid}/published", async (Guid sessionId, Guid entryId, SetJournalEntryPublishedRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new SetJournalEntryPublishedCommand(sessionId, entryId, request.Published), ct);
            return result.ToResponse();
        })
        .WithSummary("Опубликовать/скрыть запись журнала от игроков (только ГМ)");

        group.MapPut("/{sessionId:guid}/journal/{entryId:guid}/visibility", async (Guid sessionId, Guid entryId, SetJournalEntryVisibilityRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new SetJournalEntryVisibilityCommand(sessionId, entryId, request.VisibleToUserIds), ct);
            return result.ToResponse();
        })
        .WithSummary("Видимость записи журнала per-player (null — все игроки, список — только указанные)");

        group.MapDelete("/{sessionId:guid}/journal/{entryId:guid}", async (Guid sessionId, Guid entryId, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new DeleteJournalEntryCommand(sessionId, entryId), ct);
            return result.ToResponse();
        })
        .WithSummary("Удалить запись журнала (только ГМ)");
    }
}

internal sealed record SendChatRequest(string Content);
internal sealed record RollDiceRequest(string Expression, int? Dc = null, string? Label = null);
internal sealed record SetShowcaseRequest(string? ImageUrl);
internal sealed record SendWhisperRequest(Guid RecipientUserId, string Content);
internal sealed record SetTrackRequest(string TrackUrl, string? TrackTitle);
internal sealed record AudioPositionRequest(double PositionSeconds);
internal sealed record AddTokenRequest(
    string Label, string? ImageUrl, string Color, double X, double Y, Guid? OwnerUserId,
    int Width = 1, int Height = 1, string CombatantType = "None", Guid? CombatantId = null);
internal sealed record TokenPositionRequest(double X, double Y);
internal sealed record UpdateTokenStatsRequest(
    int? CurrentHp, int? Width, int? Height, int? Rotation,
    bool SetInitiative = false, int? Initiative = null,
    bool? HasDarkvision = null,
    bool? HasLowLightVision = null);
internal sealed record SetTokenVisibilityRequest(List<Guid>? VisibleToUserIds);
internal sealed record SetGridCellSizeRequest(int Px);
internal sealed record SetFogSettingsRequest(bool Enabled, int VisionRadiusFeet);
internal sealed record SetSceneEnvironmentRequest(string? TerrainTagsJson, string AmbientLighting);
internal sealed record SetWallsRequest(string? WallsJson);
internal sealed record SetLightsRequest(string? LightsJson);
internal sealed record CreateSceneRequest(string Name);
internal sealed record ApplyConditionRequest(string Slug, string Name, int? Value);
internal sealed record CreateJournalEntryRequest(
    string Title, string ContentMarkdown, Guid? ParentId = null, Guid? CampaignId = null);
internal sealed record SetJournalEntryPublishedRequest(bool Published);
internal sealed record SetJournalEntryVisibilityRequest(List<Guid>? VisibleToUserIds);
