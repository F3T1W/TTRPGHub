using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using TTRPGHub.Common;
using TTRPGHub.Common.Behaviors;
using TTRPGHub.Common.Interfaces;

namespace TTRPGHub.Application.Tests;

public class ValidationBehaviourTests
{
    public sealed record SampleRequest(string Name);

    [Fact]
    public async Task Handle_NoValidators_CallsNext()
    {
        var behaviour = new ValidationBehaviour<SampleRequest, Result>([]);
        var nextCalled = false;

        var result = await behaviour.Handle(new SampleRequest("x"), () =>
        {
            nextCalled = true;
            return Task.FromResult(Result.Success());
        }, CancellationToken.None);

        Assert.True(nextCalled);
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_ValidatorsPassWithNoFailures_CallsNext()
    {
        var validator = Substitute.For<IValidator<SampleRequest>>();
        validator.Validate(Arg.Any<ValidationContext<SampleRequest>>()).Returns(new ValidationResult());
        var behaviour = new ValidationBehaviour<SampleRequest, Result>([validator]);
        var nextCalled = false;

        var result = await behaviour.Handle(new SampleRequest("x"), () =>
        {
            nextCalled = true;
            return Task.FromResult(Result.Success());
        }, CancellationToken.None);

        Assert.True(nextCalled);
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_ValidationFailure_ReturnsResultFailureWithoutCallingNext()
    {
        var validator = Substitute.For<IValidator<SampleRequest>>();
        validator.Validate(Arg.Any<ValidationContext<SampleRequest>>())
            .Returns(new ValidationResult([new ValidationFailure("Name", "Name is required")]));
        var behaviour = new ValidationBehaviour<SampleRequest, Result>([validator]);
        var nextCalled = false;

        var result = await behaviour.Handle(new SampleRequest(""), () =>
        {
            nextCalled = true;
            return Task.FromResult(Result.Success());
        }, CancellationToken.None);

        Assert.False(nextCalled);
        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Name", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_ValidationFailure_GenericResultType_ReturnsTypedFailure()
    {
        var validator = Substitute.For<IValidator<SampleRequest>>();
        validator.Validate(Arg.Any<ValidationContext<SampleRequest>>())
            .Returns(new ValidationResult([new ValidationFailure("Name", "Name is required")]));
        var behaviour = new ValidationBehaviour<SampleRequest, Result<int>>([validator]);

        var result = await behaviour.Handle(new SampleRequest(""), () => Task.FromResult(Result<int>.Success(42)), CancellationToken.None);

        Assert.True(result.IsFailure);
    }
}

public class LoggingBehaviourTests
{
    private sealed record SampleRequest;

    [Fact]
    public async Task Handle_CallsNextAndReturnsItsResponse()
    {
        var behaviour = new LoggingBehaviour<SampleRequest, string>(NullLogger<LoggingBehaviour<SampleRequest, string>>.Instance);

        var result = await behaviour.Handle(new SampleRequest(), () => Task.FromResult("done"), CancellationToken.None);

        Assert.Equal("done", result);
    }
}

public class CachingBehaviourTests
{
    private sealed record PlainRequest;

    private sealed record CacheableRequest : ICacheableQuery
    {
        public string CacheKey => "test:key";
        public TimeSpan? Expiration => TimeSpan.FromMinutes(5);
    }

    [Fact]
    public async Task Handle_NonCacheableRequest_CallsNextWithoutTouchingCache()
    {
        var cache = Substitute.For<ICacheService>();
        var behaviour = new CachingBehaviour<PlainRequest, string>(cache, NullLogger<CachingBehaviour<PlainRequest, string>>.Instance);

        var result = await behaviour.Handle(new PlainRequest(), () => Task.FromResult("fresh"), CancellationToken.None);

        Assert.Equal("fresh", result);
        await cache.DidNotReceive().GetAsync<string>(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CacheHit_ReturnsCachedValueWithoutCallingNext()
    {
        var cache = Substitute.For<ICacheService>();
        cache.GetAsync<string>("test:key").Returns("cached-value");
        var behaviour = new CachingBehaviour<CacheableRequest, string>(cache, NullLogger<CachingBehaviour<CacheableRequest, string>>.Instance);
        var nextCalled = false;

        var result = await behaviour.Handle(new CacheableRequest(), () =>
        {
            nextCalled = true;
            return Task.FromResult("fresh");
        }, CancellationToken.None);

        Assert.Equal("cached-value", result);
        Assert.False(nextCalled);
    }

    [Fact]
    public async Task Handle_CacheMiss_CallsNextAndStoresResult()
    {
        var cache = Substitute.For<ICacheService>();
        cache.GetAsync<string>("test:key").Returns((string?)null);
        var behaviour = new CachingBehaviour<CacheableRequest, string>(cache, NullLogger<CachingBehaviour<CacheableRequest, string>>.Instance);

        var result = await behaviour.Handle(new CacheableRequest(), () => Task.FromResult("fresh"), CancellationToken.None);

        Assert.Equal("fresh", result);
        await cache.Received(1).SetAsync("test:key", "fresh", TimeSpan.FromMinutes(5), Arg.Any<CancellationToken>());
    }
}
