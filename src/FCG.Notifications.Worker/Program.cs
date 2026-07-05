using FCG.Notifications.Application.Configure;
using FCG.Notifications.Infra.Configure;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

// Logging estruturado via Serilog (sinks console + arquivo configurados em appsettings.json).
builder.Services.AddSerilog((services, configuration) => configuration
    .ReadFrom.Configuration(builder.Configuration)
    .ReadFrom.Services(services));

// Camada de aplicação: registra INotificationService -> NotificationService.
builder.Services.AddApplicationConfiguration();

// Camada de infraestrutura: MassTransit + RabbitMQ, consumers e retry exponencial.
builder.Services.AddInfrastructure(builder.Configuration);

var host = builder.Build();
host.Run();
