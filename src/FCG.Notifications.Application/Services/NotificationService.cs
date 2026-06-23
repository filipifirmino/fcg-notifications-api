using Microsoft.Extensions.Logging;

namespace FCG.Notifications.Application.Services;

public class NotificationService : INotificationService
{
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(ILogger<NotificationService> logger)
    {
        _logger = logger;
    }

    public Task SendWelcomeEmailAsync(string name, string email)
    {
        _logger.LogInformation(
            "[EMAIL SIMULADO] Boas-vindas enviado para {Email}. Olá {Name}, bem-vindo à FCG!",
            email, name);

        return Task.CompletedTask;
    }

    public Task SendPurchaseConfirmationAsync(string email, string gameTitle, decimal amount)
    {
        _logger.LogInformation(
            "[EMAIL SIMULADO] Confirmação de compra enviada para {Email}. Jogo: {GameTitle}, Valor: R$ {Amount:F2}",
            email, gameTitle, amount);

        return Task.CompletedTask;
    }

    public Task SendPurchaseRejectedAsync(string email, string gameTitle, string? reason)
    {
        _logger.LogWarning(
            "[EMAIL SIMULADO] Rejeição de compra enviada para {Email}. Jogo: {GameTitle}. Motivo: {Reason}",
            email, gameTitle, reason ?? "Não informado");

        return Task.CompletedTask;
    }
}
