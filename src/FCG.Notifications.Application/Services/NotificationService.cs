using FCG.Notifications.Domain.Entities;
using FCG.Notifications.Domain.Enums;
using FCG.Notifications.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace FCG.Notifications.Application.Services;

public class NotificationService : INotificationService
{
    private readonly ILogger<NotificationService> _logger;
    private readonly INotificationLogRepository _notificationLogRepository;

    public NotificationService(ILogger<NotificationService> logger, INotificationLogRepository notificationLogRepository)
    {
        _logger = logger;
        _notificationLogRepository = notificationLogRepository;
    }

    public async Task SendWelcomeEmailAsync(string name, string email)
    {
        var message = $"Olá {name}, bem-vindo à FCG!";

        _logger.LogInformation(
            "[EMAIL SIMULADO] Boas-vindas enviado para {Email}. Olá {Name}, bem-vindo à FCG!",
            email, name);

        await _notificationLogRepository.AddAsync(
            NotificationLog.Create(NotificationType.Welcome, email, message));
    }

    public async Task SendPurchaseConfirmationAsync(string email, string gameTitle, decimal amount)
    {
        var message = $"Confirmação de compra: {gameTitle}, Valor: R$ {amount:F2}";

        _logger.LogInformation(
            "[EMAIL SIMULADO] Confirmação de compra enviada para {Email}. Jogo: {GameTitle}, Valor: R$ {Amount:F2}",
            email, gameTitle, amount);

        await _notificationLogRepository.AddAsync(
            NotificationLog.Create(NotificationType.PurchaseConfirmation, email, message));
    }

    public async Task SendPurchaseRejectedAsync(string email, string gameTitle, string? reason)
    {
        var message = $"Rejeição de compra: {gameTitle}. Motivo: {reason ?? "Não informado"}";

        _logger.LogWarning(
            "[EMAIL SIMULADO] Rejeição de compra enviada para {Email}. Jogo: {GameTitle}. Motivo: {Reason}",
            email, gameTitle, reason ?? "Não informado");

        await _notificationLogRepository.AddAsync(
            NotificationLog.Create(NotificationType.PurchaseRejected, email, message));
    }
}
