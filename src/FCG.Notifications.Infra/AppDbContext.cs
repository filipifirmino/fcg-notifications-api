using FCG.Notifications.Domain.Entities;
using FCG.Notifications.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace FCG.Notifications.Infra;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<NotificationLog> NotificationLogs => Set<NotificationLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<NotificationLog>(entity =>
        {
            entity.HasKey(n => n.Id);
            entity.Property(n => n.Recipient).IsRequired().HasMaxLength(200);
            entity.Property(n => n.Message).IsRequired().HasMaxLength(1000);
            entity.Property(n => n.Type)
                .IsRequired()
                .HasConversion(new EnumToStringConverter<NotificationType>());
            entity.Property(n => n.SentAt).IsRequired();

            entity.HasIndex(n => n.SentAt);
        });
    }
}
