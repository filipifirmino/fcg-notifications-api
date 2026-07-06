using Bogus;
using FCG.Notifications.Domain.Interfaces;
using FCG.Notifications.Infra.Consumers;
using FCG.Events;
using FluentAssertions;
using MassTransit;
using Microsoft.Extensions.Logging;
using Moq;

namespace FCG.Notifications.Tests.Unit.Infra.Consumers;

public class UserCreatedConsumerTests
{
    private readonly Mock<INotificationService> _notificationServiceMock = new();
    private readonly Mock<ILogger<UserCreatedConsumer>> _loggerMock = new();
    private readonly UserCreatedConsumer _sut;
    private readonly Faker _faker = new("pt_BR");

    public UserCreatedConsumerTests()
    {
        _notificationServiceMock
            .Setup(s => s.SendWelcomeEmailAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        _sut = new UserCreatedConsumer(_notificationServiceMock.Object, _loggerMock.Object);
    }

    private Mock<ConsumeContext<UserCreatedEvent>> BuildContext(UserCreatedEvent @event)
    {
        var context = new Mock<ConsumeContext<UserCreatedEvent>>();
        context.SetupGet(c => c.Message).Returns(@event);
        return context;
    }

    [Fact]
    public async Task Consume_ShouldCallSendWelcomeEmailAsyncOnce()
    {
        var @event = new UserCreatedEvent
        {
            UserId = Guid.NewGuid(),
            Name = _faker.Name.FullName(),
            Email = _faker.Internet.Email(),
            CreatedAt = DateTime.UtcNow
        };

        await _sut.Consume(BuildContext(@event).Object);

        _notificationServiceMock.Verify(s => s.SendWelcomeEmailAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task Consume_ShouldPassCorrectNameToService()
    {
        var name = _faker.Name.FullName();
        var @event = new UserCreatedEvent
        {
            UserId = Guid.NewGuid(),
            Name = name,
            Email = _faker.Internet.Email(),
            CreatedAt = DateTime.UtcNow
        };

        await _sut.Consume(BuildContext(@event).Object);

        _notificationServiceMock.Verify(s => s.SendWelcomeEmailAsync(name, It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task Consume_ShouldPassCorrectEmailToService()
    {
        var email = _faker.Internet.Email();
        var @event = new UserCreatedEvent
        {
            UserId = Guid.NewGuid(),
            Name = _faker.Name.FullName(),
            Email = email,
            CreatedAt = DateTime.UtcNow
        };

        await _sut.Consume(BuildContext(@event).Object);

        _notificationServiceMock.Verify(s => s.SendWelcomeEmailAsync(It.IsAny<string>(), email), Times.Once);
    }

    [Fact]
    public async Task Consume_ShouldCompleteWithoutThrowing()
    {
        var @event = new UserCreatedEvent
        {
            UserId = Guid.NewGuid(),
            Name = _faker.Name.FullName(),
            Email = _faker.Internet.Email(),
            CreatedAt = DateTime.UtcNow
        };

        var act = async () => await _sut.Consume(BuildContext(@event).Object);

        await act.Should().NotThrowAsync();
    }
}
