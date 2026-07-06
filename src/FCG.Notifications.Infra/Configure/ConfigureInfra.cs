using FCG.Notifications.Domain.Interfaces;
using FCG.Notifications.Infra.Consumers;
using FCG.Events;
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

                // O nome do exchange precisa ser igual em todos os serviços que publicam/consomem
                // este evento. Sem isso, o MassTransit usa o namespace .NET completo do tipo como
                // nome do exchange, e cada serviço tem sua própria cópia do contrato em um namespace
                // diferente — o que faz publisher e consumer conversarem com exchanges diferentes.
                cfg.Message<UserCreatedEvent>(x => x.SetEntityName("UserCreated"));
                cfg.Message<PaymentProcessedEvent>(x => x.SetEntityName("PaymentProcessed"));

                cfg.UseMessageRetry(r =>
                    r.Exponential(5, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(5)));

                // Fila própria e exclusiva: o CatalogAPI também tem uma classe chamada
                // "PaymentProcessedConsumer". Sem um nome de fila explícito, o ConfigureEndpoints
                // nomeia a fila a partir do nome da classe do consumer, e os dois serviços cairiam
                // na MESMA fila "PaymentProcessed" — concorrendo pelas mensagens (RabbitMQ despacha
                // cada mensagem para só um consumer da fila) em vez de cada serviço receber sua
                // própria cópia do evento.
                cfg.ReceiveEndpoint("notifications-worker-payment-processed", e =>
                {
                    e.ConfigureConsumer<PaymentProcessedConsumer>(ctx);
                });

                cfg.ConfigureEndpoints(ctx);
            });
        });

        return services;
    }
}
