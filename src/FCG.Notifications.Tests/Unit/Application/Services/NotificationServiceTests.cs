using Bogus;
using FCG.Notifications.Application.Services;
using FCG.Notifications.Domain.Entities;
using FCG.Notifications.Domain.Enums;
using FCG.Notifications.Domain.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace FCG.Notifications.Tests.Unit.Application.Services;

public class NotificationServiceTests
{
    private readonly Mock<ILogger<NotificationService>> _loggerMock = new();
    private readonly Mock<INotificationLogRepository> _notificationLogRepositoryMock = new();
    private readonly NotificationService _sut;
    private readonly Faker _faker = new("pt_BR");

    public NotificationServiceTests()
    {
        _notificationLogRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<NotificationLog>()))
            .Returns(Task.CompletedTask);

        _sut = new NotificationService(_loggerMock.Object, _notificationLogRepositoryMock.Object);
    }

    private static void VerifyLog<T>(Mock<ILogger<T>> mock, LogLevel level, Times times)
    {
        mock.Verify(
            x => x.Log(
                level,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            times);
    }

    [Fact]
    public async Task SendWelcomeEmailAsync_ShouldCompleteSuccessfully()
    {
        var act = async () => await _sut.SendWelcomeEmailAsync(_faker.Name.FullName(), _faker.Internet.Email());

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SendWelcomeEmailAsync_ShouldLogAtInformationLevel()
    {
        await _sut.SendWelcomeEmailAsync(_faker.Name.FullName(), _faker.Internet.Email());

        VerifyLog(_loggerMock, LogLevel.Information, Times.Once());
    }

    [Fact]
    public async Task SendWelcomeEmailAsync_ShouldPersistNotificationLog()
    {
        var email = _faker.Internet.Email();

        await _sut.SendWelcomeEmailAsync(_faker.Name.FullName(), email);

        _notificationLogRepositoryMock.Verify(
            x => x.AddAsync(It.Is<NotificationLog>(l =>
                l.Type == NotificationType.Welcome && l.Recipient == email)),
            Times.Once());
    }

    [Fact]
    public async Task SendWelcomeEmailAsync_ShouldReturnCompletedTask()
    {
        var result = _sut.SendWelcomeEmailAsync(_faker.Name.FullName(), _faker.Internet.Email());

        result.IsCompleted.Should().BeTrue();
        await result;
    }

    [Fact]
    public async Task SendPurchaseConfirmationAsync_ShouldCompleteSuccessfully()
    {
        var act = async () => await _sut.SendPurchaseConfirmationAsync(
            _faker.Internet.Email(),
            _faker.Commerce.ProductName(),
            _faker.Random.Decimal(1, 500));

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SendPurchaseConfirmationAsync_ShouldLogAtInformationLevel()
    {
        await _sut.SendPurchaseConfirmationAsync(
            _faker.Internet.Email(),
            _faker.Commerce.ProductName(),
            _faker.Random.Decimal(1, 500));

        VerifyLog(_loggerMock, LogLevel.Information, Times.Once());
    }

    [Fact]
    public async Task SendPurchaseConfirmationAsync_ShouldPersistNotificationLog()
    {
        var email = _faker.Internet.Email();

        await _sut.SendPurchaseConfirmationAsync(email, _faker.Commerce.ProductName(), _faker.Random.Decimal(1, 500));

        _notificationLogRepositoryMock.Verify(
            x => x.AddAsync(It.Is<NotificationLog>(l =>
                l.Type == NotificationType.PurchaseConfirmation && l.Recipient == email)),
            Times.Once());
    }

    [Fact]
    public async Task SendPurchaseConfirmationAsync_ShouldReturnCompletedTask()
    {
        var result = _sut.SendPurchaseConfirmationAsync(
            _faker.Internet.Email(),
            _faker.Commerce.ProductName(),
            _faker.Random.Decimal(1, 500));

        result.IsCompleted.Should().BeTrue();
        await result;
    }

    [Fact]
    public async Task SendPurchaseRejectedAsync_ShouldCompleteSuccessfully()
    {
        var act = async () => await _sut.SendPurchaseRejectedAsync(
            _faker.Internet.Email(),
            _faker.Commerce.ProductName(),
            "Insufficient funds (simulated)");

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SendPurchaseRejectedAsync_ShouldLogAtWarningLevel()
    {
        await _sut.SendPurchaseRejectedAsync(
            _faker.Internet.Email(),
            _faker.Commerce.ProductName(),
            "Insufficient funds (simulated)");

        VerifyLog(_loggerMock, LogLevel.Warning, Times.Once());
    }

    [Fact]
    public async Task SendPurchaseRejectedAsync_ShouldPersistNotificationLog()
    {
        var email = _faker.Internet.Email();

        await _sut.SendPurchaseRejectedAsync(email, _faker.Commerce.ProductName(), "Insufficient funds (simulated)");

        _notificationLogRepositoryMock.Verify(
            x => x.AddAsync(It.Is<NotificationLog>(l =>
                l.Type == NotificationType.PurchaseRejected && l.Recipient == email)),
            Times.Once());
    }

    [Fact]
    public async Task SendPurchaseRejectedAsync_WithNullReason_ShouldCompleteSuccessfully()
    {
        var act = async () => await _sut.SendPurchaseRejectedAsync(
            _faker.Internet.Email(),
            _faker.Commerce.ProductName(),
            null);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SendPurchaseRejectedAsync_WithNullReason_ShouldLogAtWarningLevel()
    {
        await _sut.SendPurchaseRejectedAsync(
            _faker.Internet.Email(),
            _faker.Commerce.ProductName(),
            null);

        VerifyLog(_loggerMock, LogLevel.Warning, Times.Once());
    }
}
