using FCG.Notifications.Worker.Configuration;
using FCG.Notifications.Worker.Services;
using Serilog;

var host = Host.CreateDefaultBuilder(args)
    .UseSerilog((context, configuration) =>
    {
        configuration
            .ReadFrom.Configuration(context.Configuration)
            .Enrich.FromLogContext();
    })
    .ConfigureServices((hostContext, services) =>
    {
        services.AddScoped<INotificationService, NotificationService>();
        services.AddMessaging(hostContext.Configuration);
    })
    .Build();

await host.RunAsync();
