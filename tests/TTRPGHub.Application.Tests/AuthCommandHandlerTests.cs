using NSubstitute;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Features.Auth.Commands.Login;
using TTRPGHub.Features.Auth.Commands.Register;
using TTRPGHub.Interfaces;
using TTRPGHub.Repositories;
using TTRPGHub.ValueObjects;

namespace TTRPGHub.Application.Tests;

public class RegisterCommandHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly IEmailConfirmationTokenRepository _tokenRepository = Substitute.For<IEmailConfirmationTokenRepository>();
    private readonly IEmailService _emailService = Substitute.For<IEmailService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private RegisterCommandHandler CreateHandler() =>
        new(_userRepository, _passwordHasher, _tokenRepository, _emailService, _unitOfWork);

    [Fact]
    public async Task Handle_InvalidEmail_ReturnsValidationError()
    {
        var handler = CreateHandler();

        var result = await handler.Handle(new RegisterCommand("Aragorn", "not-an-email", "Password123!"), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_EmailAlreadyRegistered_ReturnsConflict()
    {
        _userRepository.ExistsByEmailAsync("aragorn@gondor.test").Returns(true);
        var handler = CreateHandler();

        var result = await handler.Handle(new RegisterCommand("Aragorn", "aragorn@gondor.test", "Password123!"), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_ValidRequest_HashesPasswordAndCreatesUser()
    {
        _userRepository.ExistsByEmailAsync(Arg.Any<string>()).Returns(false);
        _passwordHasher.Hash("Password123!").Returns("hashed-value");
        var handler = CreateHandler();

        var result = await handler.Handle(new RegisterCommand("Aragorn", "aragorn@gondor.test", "Password123!"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Aragorn", result.Value!.Username);
        Assert.Equal("aragorn@gondor.test", result.Value.Email);
        await _userRepository.Received(1).AddAsync(
            Arg.Is<User>(u => u.PasswordHash == "hashed-value"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidRequest_SendsConfirmationEmail()
    {
        _userRepository.ExistsByEmailAsync(Arg.Any<string>()).Returns(false);
        var handler = CreateHandler();

        await handler.Handle(new RegisterCommand("Aragorn", "aragorn@gondor.test", "Password123!"), CancellationToken.None);

        await _emailService.Received(1).SendEmailConfirmationAsync(
            "aragorn@gondor.test", "Aragorn", Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NormalizesEmailToLowercase()
    {
        _userRepository.ExistsByEmailAsync(Arg.Any<string>()).Returns(false);
        var handler = CreateHandler();

        var result = await handler.Handle(new RegisterCommand("Aragorn", "Aragorn@Gondor.TEST", "Password123!"), CancellationToken.None);

        Assert.Equal("aragorn@gondor.test", result.Value!.Email);
    }
}

public class LoginCommandHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly IJwtService _jwtService = Substitute.For<IJwtService>();

    private LoginCommandHandler CreateHandler() => new(_userRepository, _passwordHasher, _jwtService);

    private static User CreateUser() =>
        User.Create("Aragorn", Email.Create("aragorn@gondor.test").Value!, "hashed-value");

    [Fact]
    public async Task Handle_UnknownEmail_ReturnsValidationError()
    {
        _userRepository.GetByEmailAsync(Arg.Any<string>()).Returns((User?)null);
        var handler = CreateHandler();

        var result = await handler.Handle(new LoginCommand("ghost@nowhere.test", "whatever"), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_WrongPassword_ReturnsValidationError()
    {
        var user = CreateUser();
        _userRepository.GetByEmailAsync(Arg.Any<string>()).Returns(user);
        _passwordHasher.Verify("wrong", user.PasswordHash).Returns(false);
        var handler = CreateHandler();

        var result = await handler.Handle(new LoginCommand("aragorn@gondor.test", "wrong"), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_CorrectCredentials_ReturnsTokens()
    {
        var user = CreateUser();
        _userRepository.GetByEmailAsync(Arg.Any<string>()).Returns(user);
        _passwordHasher.Verify("correct", user.PasswordHash).Returns(true);
        _jwtService.GenerateAccessToken(user).Returns("access-token");
        _jwtService.GenerateRefreshToken().Returns("refresh-token");
        var handler = CreateHandler();

        var result = await handler.Handle(new LoginCommand("aragorn@gondor.test", "correct"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("access-token", result.Value!.AccessToken);
        Assert.Equal("refresh-token", result.Value.RefreshToken);
        Assert.Equal(user.Id.Value, result.Value.UserId);
    }

    [Fact]
    public async Task Handle_TrimsAndLowercasesEmailBeforeLookup()
    {
        _userRepository.GetByEmailAsync("aragorn@gondor.test").Returns((User?)null);
        var handler = CreateHandler();

        await handler.Handle(new LoginCommand("  Aragorn@Gondor.TEST  ", "whatever"), CancellationToken.None);

        await _userRepository.Received(1).GetByEmailAsync("aragorn@gondor.test", Arg.Any<CancellationToken>());
    }
}
