using FCG.Notifications.Domain.Interfaces;
using FCG.Notifications.Infra.Events;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace FCG.Notifications.Infra.Consumers;

public class PaymentProcessedConsumer : IConsumer<PaymentProcessedEvent>
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<PaymentProcessedConsumer> _logger;

    public PaymentProcessedConsumer(INotificationService notificationService, ILogger<PaymentProcessedConsumer> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<PaymentProcessedEvent> context)
    {
        var payment = context.Message;

        _logger.LogInformation(
            "Evento PaymentProcessed recebido: OrderId={OrderId}, Status={Status}, Email={Email}",
            payment.OrderId, payment.Status, payment.UserEmail);

        if (payment.Status == PaymentStatus.Approved)
        {
            await _notificationService.SendPurchaseConfirmationAsync(
                payment.UserEmail,
                payment.GameTitle,
                payment.Amount);
        }
        else
        {
            await _notificationService.SendPurchaseRejectedAsync(
                payment.UserEmail,
                payment.GameTitle,
                payment.Reason);
        }
    }
}
