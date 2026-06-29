using FluentValidation;
using MediatR;

namespace TTRPGHub.Common.Behaviors;

public sealed class ValidationBehaviour<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        if (!validators.Any())
            return await next();

        var context = new ValidationContext<TRequest>(request);
        var failures = validators
            .Select(v => v.Validate(context))
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count == 0)
            return await next();

        var errors = failures
            .Select(f => Error.Validation(f.PropertyName, f.ErrorMessage))
            .ToList();

        return CreateValidationResult<TResponse>(errors);
    }

    private static TResponse CreateValidationResult<T>(List<Error> errors)
    {
        var firstError = errors.First();

        if (typeof(T) == typeof(Result))
            return (TResponse)(object)Result.Failure(firstError);

        var resultType = typeof(T).GetGenericArguments().First();
        var failureMethod = typeof(Result<>)
            .MakeGenericType(resultType)
            .GetMethod(nameof(Result<>.Failure))!;

        return (TResponse)failureMethod.Invoke(null, [firstError])!;
    }
}
