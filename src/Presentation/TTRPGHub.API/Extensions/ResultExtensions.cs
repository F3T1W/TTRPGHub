using TTRPGHub.Domain.Common;

namespace TTRPGHub.API.Extensions;

internal static class ResultExtensions
{
    public static IResult ToResponse<T>(this Result<T> result) =>
        result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error!.ToProblem();

    public static IResult ToResponse(this Result result) =>
        result.IsSuccess
            ? Results.NoContent()
            : result.Error!.ToProblem();

    private static IResult ToProblem(this Error error)
    {
        var status = error.Code switch
        {
            var c when c.EndsWith(".NotFound") => StatusCodes.Status404NotFound,
            var c when c.EndsWith(".Conflict") => StatusCodes.Status409Conflict,
            "Auth.Unauthorized" => StatusCodes.Status401Unauthorized,
            var c when c.StartsWith("Validation.") => StatusCodes.Status422UnprocessableEntity,
            _ => StatusCodes.Status400BadRequest
        };

        return Results.Problem(
            title: error.Code,
            detail: error.Description,
            statusCode: status);
    }
}
