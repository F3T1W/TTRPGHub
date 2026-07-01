using MediatR;
using TTRPGHub.Extensions;
using TTRPGHub.Features.GameTable.Commands.AddTableToken;
using TTRPGHub.Features.GameTable.Commands.ClearTableAudio;
using TTRPGHub.Features.GameTable.Commands.MoveTableToken;
using TTRPGHub.Features.GameTable.Commands.PauseTableAudio;
using TTRPGHub.Features.GameTable.Commands.PlayTableAudio;
using TTRPGHub.Features.GameTable.Commands.RemoveTableToken;
using TTRPGHub.Features.GameTable.Commands.RollDice;
using TTRPGHub.Features.GameTable.Commands.SeekTableAudio;
using TTRPGHub.Features.GameTable.Commands.SendChatMessage;
using TTRPGHub.Features.GameTable.Commands.SendWhisper;
using TTRPGHub.Features.GameTable.Commands.SetShowcaseImage;
using TTRPGHub.Features.GameTable.Commands.SetTableTrack;
using TTRPGHub.Features.GameTable.Commands.UploadShowcaseImage;
using TTRPGHub.Features.GameTable.Commands.UploadTableTrack;
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
            var result = await sender.Send(new RollDiceCommand(sessionId, request.Expression), ct);
            return result.ToResponse();
        })
        .WithSummary("Бросить кубики");

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
                request.X, request.Y, request.OwnerUserId), ct);
            return result.ToResponse();
        })
        .WithSummary("Добавить жетон на карту (только ГМ)");

        group.MapPut("/{sessionId:guid}/tokens/{tokenId:guid}/position", async (Guid sessionId, Guid tokenId, TokenPositionRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new MoveTableTokenCommand(sessionId, tokenId, request.X, request.Y), ct);
            return result.ToResponse();
        })
        .WithSummary("Переместить жетон");

        group.MapDelete("/{sessionId:guid}/tokens/{tokenId:guid}", async (Guid sessionId, Guid tokenId, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new RemoveTableTokenCommand(sessionId, tokenId), ct);
            return result.ToResponse();
        })
        .WithSummary("Удалить жетон (только ГМ)");
    }
}

internal sealed record SendChatRequest(string Content);
internal sealed record RollDiceRequest(string Expression);
internal sealed record SetShowcaseRequest(string? ImageUrl);
internal sealed record SendWhisperRequest(Guid RecipientUserId, string Content);
internal sealed record SetTrackRequest(string TrackUrl, string? TrackTitle);
internal sealed record AudioPositionRequest(double PositionSeconds);
internal sealed record AddTokenRequest(string Label, string? ImageUrl, string Color, double X, double Y, Guid? OwnerUserId);
internal sealed record TokenPositionRequest(double X, double Y);
