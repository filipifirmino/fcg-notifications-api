using Bogus;
using FCG.Notifications.Domain.Interfaces;
using FCG.Events;
using FCG.Notifications.Tests.Integration.Config;
using FluentAssertions;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace FCG.Notifications.Tests.Integration.Consumers;

public class ConsumersIntegrationTests : IClassFixture<NotificationsTestFactory>
{
    private readonly ITestHarness _harness;
    private readonly Mock<INotificationService> _notificationServiceMock;
    private readonly Faker _faker = new("pt_BR");

    public ConsumersIntegrationTests(NotificationsTestFactory factory)
    {
        _harness = factory.GetTestHarness();
        _notificationServiceMock = factory.NotificationServiceMock;
        _notificationServiceMock.Invocations.Clear();
    }

    [Fact]
    public async Task UserCreatedConsumer_ShouldReceiveUserCreatedEvent()
    {
        var @event = new UserCreatedEvent
        {
            UserId = Guid.NewGuid(),
            Name = _faker.Name.FullName(),
            Email = _faker.Internet.Email(),
            CreatedAt = DateTime.UtcNow
        };

        await _harness.Bus.Publish(@event);

        (await _harness.Consumed.Any<UserCreatedEvent>()).Should().BeTrue();
    }

    [Fact]
    public async Task UserCreatedConsumer_ShouldCallSendWelcomeEmailAsync()
    {
        var name = _faker.Name.FullName();
        var email = _faker.Internet.Email();
        var @event = new UserCreatedEvent
        {
            UserId = Guid.NewGuid(),
            Name = name,
            Email = email,
            CreatedAt = DateTime.UtcNow
        };

        await _harness.Bus.Publish(@event);
        await Task.Delay(500);

        _notificationServiceMock.Verify(
            s => s.SendWelcomeEmailAsync(name, email),
            Times.Once);
    }

    [Fact]
    public async Task PaymentProcessedConsumer_WhenApproved_ShouldReceiveEvent()
    {
        var @event = new PaymentProcessedEvent
        {
            OrderId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            GameId = Guid.NewGuid(),
            GameTitle = _faker.Commerce.ProductName(),
            UserEmail = _faker.Internet.Email(),
            Amount = _faker.Random.Decimal(1, 300),
            Status = PaymentStatus.Approved,
            ProcessedAt = DateTime.UtcNow
        };

        await _harness.Bus.Publish(@event);

        (await _harness.Consumed.Any<PaymentProcessedEvent>()).Should().BeTrue();
    }

    [Fact]
    public async Task PaymentProcessedConsumer_WhenApproved_ShouldCallSendPurchaseConfirmationAsync()
    {
        var email = _faker.Internet.Email();
        var gameTitle = _faker.Commerce.ProductName();
        var amount = _faker.Random.Decimal(1, 300);
        var @event = new PaymentProcessedEvent
        {
            OrderId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            GameId = Guid.NewGuid(),
            GameTitle = gameTitle,
            UserEmail = email,
            Amount = amount,
            Status = PaymentStatus.Approved,
            ProcessedAt = DateTime.UtcNow
        };

        await _harness.Bus.Publish(@event);
        await Task.Delay(500);

        _notificationServiceMock.Verify(
            s => s.SendPurchaseConfirmationAsync(email, gameTitle, amount),
            Times.Once);
    }

    [Fact]
    public async Task PaymentProcessedConsumer_WhenRejected_ShouldCallSendPurchaseRejectedAsync()
    {
        var email = _faker.Internet.Email();
        var gameTitle = _faker.Commerce.ProductName();
        var reason = "Insufficient funds (simulated)";
        var @event = new PaymentProcessedEvent
        {
            OrderId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            GameId = Guid.NewGuid(),
            GameTitle = gameTitle,
            UserEmail = email,
            Amount = 0m,
            Status = PaymentStatus.Rejected,
            Reason = reason,
            ProcessedAt = DateTime.UtcNow
        };

        await _harness.Bus.Publish(@event);
        await Task.Delay(500);

        _notificationServiceMock.Verify(
            s => s.SendPurchaseRejectedAsync(email, gameTitle, reason),
            Times.Once);
    }

    [Fact]
    public async Task PaymentProcessedConsumer_WhenRejected_ShouldReceiveEvent()
    {
        var @event = new PaymentProcessedEvent
        {
            OrderId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            GameId = Guid.NewGuid(),
            GameTitle = _faker.Commerce.ProductName(),
            UserEmail = _faker.Internet.Email(),
            Amount = 0m,
            Status = PaymentStatus.Rejected,
            Reason = "Insufficient funds (simulated)",
            ProcessedAt = DateTime.UtcNow
        };

        await _harness.Bus.Publish(@event);

        (await _harness.Consumed.Any<PaymentProcessedEvent>()).Should().BeTrue();
    }
}
