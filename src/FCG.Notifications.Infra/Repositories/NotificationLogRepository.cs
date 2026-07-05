using FCG.Notifications.Domain.Entities;
using FCG.Notifications.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FCG.Notifications.Infra.Repositories;

public class NotificationLogRepository : INotificationLogRepository
{
    private readonly AppDbContext _context;

    public NotificationLogRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(NotificationLog log)
    {
        await _context.NotificationLogs.AddAsync(log);
        await _context.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<NotificationLog>> GetRecentAsync(int take = 50)
        => await _context.NotificationLogs
            .OrderByDescending(n => n.SentAt)
            .Take(take)
            .ToListAsync();
}
