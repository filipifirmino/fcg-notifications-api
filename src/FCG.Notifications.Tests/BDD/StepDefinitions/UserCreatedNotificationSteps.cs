using FCG.Notifications.Domain.Interfaces;
using FCG.Notifications.Infra.Consumers;
using FCG.Events;
using MassTransit;
using Microsoft.Extensions.Logging;
using Moq;
using Reqnroll;

namespace FCG.Notifications.Tests.BDD.StepDefinitions;

[Binding]
public class UserCreatedNotificationSteps
{
    private Mock<INotificationService> _notificationServiceMock = new();
    private Mock<ILogger<UserCreatedConsumer>> _loggerMock = new();
    private UserCreatedConsumer _consumer = null!;
    private UserCreatedEvent _event = null!;

    [BeforeScenario]
    public void Setup()
    {
        _notificationServiceMock = new Mock<INotificationService>();
        _loggerMock = new Mock<ILogger<UserCreatedConsumer>>();
        _notificationServiceMock
            .Setup(s => s.SendWelcomeEmailAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        _consumer = new UserCreatedConsumer(_notificationServiceMock.Object, _loggerMock.Object);
    }

    [Given(@"a new user with name ""(.*)"" and email ""(.*)"" was created")]
    public void GivenANewUserWasCreated(string name, string email)
    {
        _event = new UserCreatedEvent
        {
            UserId = Guid.NewGuid(),
            Name = name,
            Email = email,
            CreatedAt = DateTime.UtcNow
        };
    }

    [When(@"the UserCreated event is consumed")]
    public async Task WhenTheUserCreatedEventIsConsumed()
    {
        var context = new Mock<ConsumeContext<UserCreatedEvent>>();
        context.SetupGet(c => c.Message).Returns(_event);
        await _consumer.Consume(context.Object);
    }

    [Then(@"SendWelcomeEmailAsync should be called once")]
    public void ThenSendWelcomeEmailAsyncShouldBeCalledOnce()
    {
        _notificationServiceMock.Verify(
            s => s.SendWelcomeEmailAsync(It.IsAny<string>(), It.IsAny<string>()),
            Times.Once);
    }

    [Then(@"it should be called with name ""(.*)"" and email ""(.*)""")]
    public void ThenItShouldBeCalledWithNameAndEmail(string name, string email)
    {
        _notificationServiceMock.Verify(s => s.SendWelcomeEmailAsync(name, email), Times.Once);
    }

    [Then(@"SendWelcomeEmailAsync should be called with name ""(.*)""")]
    public void ThenSendWelcomeEmailAsyncShouldBeCalledWithName(string name)
    {
        _notificationServiceMock.Verify(s => s.SendWelcomeEmailAsync(name, It.IsAny<string>()), Times.Once);
    }

    [Then(@"SendWelcomeEmailAsync should be called with email ""(.*)""")]
    public void ThenSendWelcomeEmailAsyncShouldBeCalledWithEmail(string email)
    {
        _notificationServiceMock.Verify(s => s.SendWelcomeEmailAsync(It.IsAny<string>(), email), Times.Once);
    }
}
