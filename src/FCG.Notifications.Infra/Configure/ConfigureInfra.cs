using FCG.Notifications.Domain.Interfaces;
using FCG.Notifications.Infra.Consumers;
using FCG.Notifications.Infra.Repositories;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FCG.Notifications.Infra.Configure;

public static class ConfigureInfra
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(opts => opts
            .UseNpgsql(configuration.GetConnectionString("Postgres"))
            .UseSnakeCaseNamingConvention());

        services.AddScoped<INotificationLogRepository, NotificationLogRepository>();

        services.AddMassTransit(x =>
        {
            x.AddConsumer<UserCreatedConsumer>();
            x.AddConsumer<PaymentProcessedConsumer>();

            x.UsingRabbitMq((ctx, cfg) =>
            {
                cfg.Host(configuration["RabbitMq:Host"] ?? "localhost", h =>
                {
                    h.Username(configuration["RabbitMq:Username"] ?? "guest");
                    h.Password(configuration["RabbitMq:Password"] ?? "guest");
                });

                cfg.UseMessageRetry(r =>
                    r.Exponential(5, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(5)));

                cfg.ConfigureEndpoints(ctx);
            });
        });

        return services;
    }
}
