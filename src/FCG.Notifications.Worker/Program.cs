using FCG.Notifications.Application.Configure;
using FCG.Notifications.Infra.Configure;
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
        services.AddApplicationConfiguration();
        services.AddInfrastructure(hostContext.Configuration);
    })
    .Build();

await host.RunAsync();
