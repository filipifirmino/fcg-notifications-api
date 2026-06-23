using FCG.Notifications.Worker.Events;
using FCG.Notifications.Worker.Services;
using MassTransit;

namespace FCG.Notifications.Worker.Consumers;

public class UserCreatedConsumer : IConsumer<UserCreatedEvent>
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<UserCreatedConsumer> _logger;

    public UserCreatedConsumer(INotificationService notificationService, ILogger<UserCreatedConsumer> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<UserCreatedEvent> context)
    {
        var user = context.Message;

        _logger.LogInformation(
            "Evento UserCreated recebido para UserId={UserId}, Email={Email}",
            user.UserId, user.Email);

        await _notificationService.SendWelcomeEmailAsync(user.Name, user.Email);
    }
}
