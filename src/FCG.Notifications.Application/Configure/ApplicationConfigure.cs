using FCG.Notifications.Application.Services;
using FCG.Notifications.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace FCG.Notifications.Application.Configure;

public static class ApplicationConfigure
{
    public static IServiceCollection AddApplicationConfiguration(this IServiceCollection services)
    {
        services.AddScoped<INotificationService, NotificationService>();
        return services;
    }
}
