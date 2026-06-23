namespace FCG.Notifications.Application.Services;

public interface INotificationService
{
    Task SendWelcomeEmailAsync(string name, string email);
    Task SendPurchaseConfirmationAsync(string email, string gameTitle, decimal amount);
    Task SendPurchaseRejectedAsync(string email, string gameTitle, string? reason);
}
