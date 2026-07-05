using FCG.Notifications.Domain.Entities;

namespace FCG.Notifications.Domain.Interfaces;

public interface INotificationLogRepository
{
    Task AddAsync(NotificationLog log);
    Task<IReadOnlyList<NotificationLog>> GetRecentAsync(int take = 50);
}
