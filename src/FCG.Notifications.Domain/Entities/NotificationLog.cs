using FCG.Notifications.Domain.Enums;

namespace FCG.Notifications.Domain.Entities;

public class NotificationLog
{
    private NotificationLog() { }

    public Guid Id { get; private set; }
    public NotificationType Type { get; private set; }
    public string Recipient { get; private set; } = string.Empty;
    public string Message { get; private set; } = string.Empty;
    public DateTime SentAt { get; private set; }

    public static NotificationLog Create(NotificationType type, string recipient, string message)
    {
        return new NotificationLog
        {
            Id = Guid.NewGuid(),
            Type = type,
            Recipient = recipient,
            Message = message,
            SentAt = DateTime.UtcNow
        };
    }
}
