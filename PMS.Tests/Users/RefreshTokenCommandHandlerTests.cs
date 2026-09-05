using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using PMS.Application.Abstractions.Authentication;
using PMS.Application.Abstractions.Data;
using PMS.Application.Users;
using PMS.Application.Users.RefreshToken;
using PMS.Domain.Users;
using PMS.Infrastructure.Database;
using PMS.SharedKernel;
using RefreshTokenEntity = PMS.Domain.Users.RefreshToken;

namespace PMS.UnitTests.Users;

public class RefreshTokenCommandHandlerTests
{
    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static IRepository<RefreshTokenEntity> CreateRepository(ApplicationDbContext context)
    {
        var repository = Substitute.For<IRepository<RefreshTokenEntity>>();

        repository.DeleteAsync(Arg.Any<RefreshTokenEntity>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                context.RefreshTokens.Remove(callInfo.ArgAt<RefreshTokenEntity>(0));
                return Task.CompletedTask;
            });

        repository.AddAsync(Arg.Any<RefreshTokenEntity>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                RefreshTokenEntity token = callInfo.ArgAt<RefreshTokenEntity>(0);
                context.RefreshTokens.Add(token);
                return Task.FromResult(token);
            });

        return repository;
    }

    private static IUnitOfWork CreateUnitOfWork(ApplicationDbContext context)
    {
        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.CommitAsync(Arg.Any<CancellationToken>())
            .Returns(callInfo => context.SaveChangesAsync(callInfo.ArgAt<CancellationToken>(0)));

        return unitOfWork;
    }

    [Fact]
    public async Task Handle_Should_RotateTokenAndRaiseDomainEvent_WhenTokenIsValid()
    {
        // Arrange
        await using ApplicationDbContext context = CreateDbContext();
        DateTime utcNow = new(2026, 9, 5, 8, 0, 0, DateTimeKind.Utc);
        User user = CreateUser(isActive: true);
        RefreshTokenEntity existingToken = CreateToken(user, "old_refresh_token", utcNow.AddDays(1));
        context.AddRange(user, existingToken);
        await context.SaveChangesAsync();

        IRepository<RefreshTokenEntity> repository = CreateRepository(context);
        IUnitOfWork unitOfWork = CreateUnitOfWork(context);
        var tokenProvider = Substitute.For<ITokenProvider>();
        tokenProvider.CreateAccessToken(user).Returns("new_access_token");
        tokenProvider.CreateRefreshToken().Returns("new_refresh_token");
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(utcNow);
        var handler = new RefreshTokenCommandHandler(
            context,
            repository,
            unitOfWork,
            tokenProvider,
            dateTimeProvider);

        // Act
        Result<AccessTokenResponse> result = await handler.Handle(
            new RefreshTokenCommand(existingToken.Token),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().Be("new_access_token");
        result.Value.RefreshToken.Should().Be("new_refresh_token");
        (await context.RefreshTokens.AnyAsync(token => token.Token == "old_refresh_token")).Should().BeFalse();

        RefreshTokenEntity replacement = await context.RefreshTokens.SingleAsync();
        replacement.Token.Should().Be("new_refresh_token");
        replacement.UserId.Should().Be(user.Id);
        replacement.ExpiresOnUtc.Should().Be(utcNow.AddDays(7));
        user.DomainEvents.Should().ContainSingle(domainEvent => domainEvent is RefreshTokenRotatedDomainEvent);
        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ReturnUnauthorizedWithoutCommit_WhenTokenDoesNotExist()
    {
        // Arrange
        await using ApplicationDbContext context = CreateDbContext();
        IRepository<RefreshTokenEntity> repository = CreateRepository(context);
        IUnitOfWork unitOfWork = CreateUnitOfWork(context);
        var tokenProvider = Substitute.For<ITokenProvider>();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        var handler = new RefreshTokenCommandHandler(
            context,
            repository,
            unitOfWork,
            tokenProvider,
            dateTimeProvider);

        // Act
        Result<AccessTokenResponse> result = await handler.Handle(
            new RefreshTokenCommand("unknown_refresh_token"),
            CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserErrors.InvalidRefreshToken);
        await unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_DeleteTokenAndReturnUnauthorized_WhenTokenIsExpired()
    {
        // Arrange
        await using ApplicationDbContext context = CreateDbContext();
        DateTime utcNow = new(2026, 9, 5, 8, 0, 0, DateTimeKind.Utc);
        User user = CreateUser(isActive: true);
        RefreshTokenEntity expiredToken = CreateToken(user, "expired_refresh_token", utcNow);
        context.AddRange(user, expiredToken);
        await context.SaveChangesAsync();

        IRepository<RefreshTokenEntity> repository = CreateRepository(context);
        IUnitOfWork unitOfWork = CreateUnitOfWork(context);
        var tokenProvider = Substitute.For<ITokenProvider>();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(utcNow);
        var handler = new RefreshTokenCommandHandler(
            context,
            repository,
            unitOfWork,
            tokenProvider,
            dateTimeProvider);

        // Act
        Result<AccessTokenResponse> result = await handler.Handle(
            new RefreshTokenCommand(expiredToken.Token),
            CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserErrors.InvalidRefreshToken);
        (await context.RefreshTokens.AnyAsync()).Should().BeFalse();
        user.DomainEvents.Should().ContainSingle(domainEvent => domainEvent is RefreshTokenInvalidatedDomainEvent);
        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_DeleteTokenAndReturnUnauthorized_WhenUserIsInactive()
    {
        // Arrange
        await using ApplicationDbContext context = CreateDbContext();
        DateTime utcNow = new(2026, 9, 5, 8, 0, 0, DateTimeKind.Utc);
        User user = CreateUser(isActive: false);
        RefreshTokenEntity existingToken = CreateToken(user, "inactive_user_token", utcNow.AddDays(1));
        context.AddRange(user, existingToken);
        await context.SaveChangesAsync();

        IRepository<RefreshTokenEntity> repository = CreateRepository(context);
        IUnitOfWork unitOfWork = CreateUnitOfWork(context);
        var tokenProvider = Substitute.For<ITokenProvider>();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(utcNow);
        var handler = new RefreshTokenCommandHandler(
            context,
            repository,
            unitOfWork,
            tokenProvider,
            dateTimeProvider);

        // Act
        Result<AccessTokenResponse> result = await handler.Handle(
            new RefreshTokenCommand(existingToken.Token),
            CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserErrors.InvalidRefreshToken);
        (await context.RefreshTokens.AnyAsync()).Should().BeFalse();
        user.DomainEvents.Should().ContainSingle(domainEvent => domainEvent is RefreshTokenInvalidatedDomainEvent);
        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ReturnUnauthorized_WhenRotatedTokenIsReused()
    {
        // Arrange
        await using ApplicationDbContext context = CreateDbContext();
        DateTime utcNow = new(2026, 9, 5, 8, 0, 0, DateTimeKind.Utc);
        User user = CreateUser(isActive: true);
        RefreshTokenEntity existingToken = CreateToken(user, "single_use_token", utcNow.AddDays(1));
        context.AddRange(user, existingToken);
        await context.SaveChangesAsync();

        IRepository<RefreshTokenEntity> repository = CreateRepository(context);
        IUnitOfWork unitOfWork = CreateUnitOfWork(context);
        var tokenProvider = Substitute.For<ITokenProvider>();
        tokenProvider.CreateAccessToken(user).Returns("new_access_token");
        tokenProvider.CreateRefreshToken().Returns("replacement_token");
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(utcNow);
        var handler = new RefreshTokenCommandHandler(
            context,
            repository,
            unitOfWork,
            tokenProvider,
            dateTimeProvider);

        await handler.Handle(new RefreshTokenCommand(existingToken.Token), CancellationToken.None);

        // Act
        Result<AccessTokenResponse> replayResult = await handler.Handle(
            new RefreshTokenCommand(existingToken.Token),
            CancellationToken.None);

        // Assert
        replayResult.IsFailure.Should().BeTrue();
        replayResult.Error.Should().Be(UserErrors.InvalidRefreshToken);
        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    private static User CreateUser(bool isActive) => new()
    {
        Id = Guid.NewGuid(),
        FirstName = "Refresh",
        LastName = "Tester",
        Email = $"refresh.{Guid.NewGuid():N}@example.com",
        PasswordHash = "hashed_password",
        IsActive = isActive
    };

    private static RefreshTokenEntity CreateToken(User user, string token, DateTime expiresOnUtc) => new()
    {
        Id = Guid.NewGuid(),
        Token = token,
        UserId = user.Id,
        User = user,
        ExpiresOnUtc = expiresOnUtc
    };
}
