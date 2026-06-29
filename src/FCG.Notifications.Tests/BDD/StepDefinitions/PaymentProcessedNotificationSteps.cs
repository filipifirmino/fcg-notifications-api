using FCG.Notifications.Domain.Interfaces;
using FCG.Notifications.Infra.Consumers;
using FCG.Notifications.Infra.Events;
using MassTransit;
using Microsoft.Extensions.Logging;
using Moq;
using Reqnroll;

namespace FCG.Notifications.Tests.BDD.StepDefinitions;

[Binding]
public class PaymentProcessedNotificationSteps
{
    private Mock<INotificationService> _notificationServiceMock = new();
    private Mock<ILogger<PaymentProcessedConsumer>> _loggerMock = new();
    private PaymentProcessedConsumer _consumer = null!;
    private PaymentProcessedEvent _event = null!;

    [BeforeScenario]
    public void Setup()
    {
        _notificationServiceMock = new Mock<INotificationService>();
        _loggerMock = new Mock<ILogger<PaymentProcessedConsumer>>();
        _notificationServiceMock
            .Setup(s => s.SendPurchaseConfirmationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<decimal>()))
            .Returns(Task.CompletedTask);
        _notificationServiceMock
            .Setup(s => s.SendPurchaseRejectedAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);
        _consumer = new PaymentProcessedConsumer(_notificationServiceMock.Object, _loggerMock.Object);
    }

    [Given(@"a payment for game ""(.*)"" to email ""(.*)"" with amount (.*) was approved")]
    public void GivenAPaymentWasApproved(string game, string email, decimal amount)
    {
        _event = new PaymentProcessedEvent
        {
            OrderId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            GameId = Guid.NewGuid(),
            GameTitle = game,
            UserEmail = email,
            Amount = amount,
            Status = PaymentStatus.Approved,
            Reason = null,
            ProcessedAt = DateTime.UtcNow
        };
    }

    [Given(@"a payment for game ""(.*)"" to email ""(.*)"" was rejected with reason ""(.*)""")]
    public void GivenAPaymentWasRejected(string game, string email, string reason)
    {
        _event = new PaymentProcessedEvent
        {
            OrderId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            GameId = Guid.NewGuid(),
            GameTitle = game,
            UserEmail = email,
            Amount = 0m,
            Status = PaymentStatus.Rejected,
            Reason = reason,
            ProcessedAt = DateTime.UtcNow
        };
    }

    [When(@"the PaymentProcessed event is consumed")]
    public async Task WhenThePaymentProcessedEventIsConsumed()
    {
        var context = new Mock<ConsumeContext<PaymentProcessedEvent>>();
        context.SetupGet(c => c.Message).Returns(_event);
        await _consumer.Consume(context.Object);
    }

    [Then(@"SendPurchaseConfirmationAsync should be called once")]
    public void ThenSendPurchaseConfirmationAsyncShouldBeCalledOnce()
    {
        _notificationServiceMock.Verify(
            s => s.SendPurchaseConfirmationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<decimal>()),
            Times.Once);
    }

    [Then(@"it should be called with email ""(.*)"" and game ""(.*)"" and amount (.*)")]
    public void ThenItShouldBeCalledWithEmailGameAndAmount(string email, string game, decimal amount)
    {
        _notificationServiceMock.Verify(s => s.SendPurchaseConfirmationAsync(email, game, amount), Times.Once);
    }

    [Then(@"SendPurchaseRejectedAsync should be called once")]
    public void ThenSendPurchaseRejectedAsyncShouldBeCalledOnce()
    {
        _notificationServiceMock.Verify(
            s => s.SendPurchaseRejectedAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>()),
            Times.Once);
    }

    [Then(@"it should be called with email ""(.*)"" and game ""(.*)"" and reason ""(.*)""")]
    public void ThenItShouldBeCalledWithEmailGameAndReason(string email, string game, string reason)
    {
        _notificationServiceMock.Verify(s => s.SendPurchaseRejectedAsync(email, game, reason), Times.Once);
    }

    [Then(@"SendPurchaseConfirmationAsync should not be called")]
    public void ThenSendPurchaseConfirmationAsyncShouldNotBeCalled()
    {
        _notificationServiceMock.Verify(
            s => s.SendPurchaseConfirmationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<decimal>()),
            Times.Never);
    }

    [Then(@"SendPurchaseRejectedAsync should not be called")]
    public void ThenSendPurchaseRejectedAsyncShouldNotBeCalled()
    {
        _notificationServiceMock.Verify(
            s => s.SendPurchaseRejectedAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>()),
            Times.Never);
    }
}
