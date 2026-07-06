using Bogus;
using FCG.Notifications.Domain.Interfaces;
using FCG.Notifications.Infra.Consumers;
using FCG.Events;
using FluentAssertions;
using MassTransit;
using Microsoft.Extensions.Logging;
using Moq;

namespace FCG.Notifications.Tests.Unit.Infra.Consumers;

public class PaymentProcessedConsumerTests
{
    private readonly Mock<INotificationService> _notificationServiceMock = new();
    private readonly Mock<ILogger<PaymentProcessedConsumer>> _loggerMock = new();
    private readonly PaymentProcessedConsumer _sut;
    private readonly Faker _faker = new("pt_BR");

    public PaymentProcessedConsumerTests()
    {
        _notificationServiceMock
            .Setup(s => s.SendPurchaseConfirmationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<decimal>()))
            .Returns(Task.CompletedTask);

        _notificationServiceMock
            .Setup(s => s.SendPurchaseRejectedAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);

        _sut = new PaymentProcessedConsumer(_notificationServiceMock.Object, _loggerMock.Object);
    }

    private Mock<ConsumeContext<PaymentProcessedEvent>> BuildContext(PaymentProcessedEvent @event)
    {
        var context = new Mock<ConsumeContext<PaymentProcessedEvent>>();
        context.SetupGet(c => c.Message).Returns(@event);
        return context;
    }

    private PaymentProcessedEvent BuildEvent(PaymentStatus status, string? reason = null) =>
        new()
        {
            OrderId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            GameId = Guid.NewGuid(),
            GameTitle = _faker.Commerce.ProductName(),
            UserEmail = _faker.Internet.Email(),
            Amount = _faker.Random.Decimal(1, 300),
            Status = status,
            Reason = reason,
            ProcessedAt = DateTime.UtcNow
        };

    [Fact]
    public async Task Consume_WhenApproved_ShouldCallSendPurchaseConfirmationAsync()
    {
        var @event = BuildEvent(PaymentStatus.Approved);

        await _sut.Consume(BuildContext(@event).Object);

        _notificationServiceMock.Verify(
            s => s.SendPurchaseConfirmationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<decimal>()),
            Times.Once);
    }

    [Fact]
    public async Task Consume_WhenApproved_ShouldNotCallSendPurchaseRejectedAsync()
    {
        var @event = BuildEvent(PaymentStatus.Approved);

        await _sut.Consume(BuildContext(@event).Object);

        _notificationServiceMock.Verify(
            s => s.SendPurchaseRejectedAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>()),
            Times.Never);
    }

    [Fact]
    public async Task Consume_WhenRejected_ShouldCallSendPurchaseRejectedAsync()
    {
        var @event = BuildEvent(PaymentStatus.Rejected, "Insufficient funds (simulated)");

        await _sut.Consume(BuildContext(@event).Object);

        _notificationServiceMock.Verify(
            s => s.SendPurchaseRejectedAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>()),
            Times.Once);
    }

    [Fact]
    public async Task Consume_WhenRejected_ShouldNotCallSendPurchaseConfirmationAsync()
    {
        var @event = BuildEvent(PaymentStatus.Rejected, "Insufficient funds (simulated)");

        await _sut.Consume(BuildContext(@event).Object);

        _notificationServiceMock.Verify(
            s => s.SendPurchaseConfirmationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<decimal>()),
            Times.Never);
    }

    [Fact]
    public async Task Consume_WhenApproved_ShouldPassCorrectEmailGameTitleAndAmount()
    {
        var email = _faker.Internet.Email();
        var gameTitle = "God of War";
        var amount = 199.90m;
        var @event = BuildEvent(PaymentStatus.Approved) with
        {
            UserEmail = email,
            GameTitle = gameTitle,
            Amount = amount
        };

        await _sut.Consume(BuildContext(@event).Object);

        _notificationServiceMock.Verify(s => s.SendPurchaseConfirmationAsync(email, gameTitle, amount), Times.Once);
    }

    [Fact]
    public async Task Consume_WhenRejected_ShouldPassCorrectEmailGameTitleAndReason()
    {
        var email = _faker.Internet.Email();
        var gameTitle = "Cyberpunk 2077";
        var reason = "Insufficient funds (simulated)";
        var @event = BuildEvent(PaymentStatus.Rejected, reason) with
        {
            UserEmail = email,
            GameTitle = gameTitle
        };

        await _sut.Consume(BuildContext(@event).Object);

        _notificationServiceMock.Verify(s => s.SendPurchaseRejectedAsync(email, gameTitle, reason), Times.Once);
    }

    [Fact]
    public async Task Consume_WhenRejected_WithNullReason_ShouldPassNullToService()
    {
        var @event = BuildEvent(PaymentStatus.Rejected, null);

        await _sut.Consume(BuildContext(@event).Object);

        _notificationServiceMock.Verify(
            s => s.SendPurchaseRejectedAsync(It.IsAny<string>(), It.IsAny<string>(), null),
            Times.Once);
    }

    [Fact]
    public async Task Consume_ShouldCompleteWithoutThrowing()
    {
        var @event = BuildEvent(PaymentStatus.Approved);

        var act = async () => await _sut.Consume(BuildContext(@event).Object);

        await act.Should().NotThrowAsync();
    }
}
