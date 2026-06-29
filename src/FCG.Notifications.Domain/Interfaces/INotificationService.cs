namespace FCG.Notifications.Domain.Interfaces;

public interface INotificationService
{
    Task SendWelcomeEmailAsync(string name, string email);
    Task SendPurchaseConfirmationAsync(string email, string gameTitle, decimal amount);
    Task SendPurchaseRejectedAsync(string email, string gameTitle, string? reason);
}
