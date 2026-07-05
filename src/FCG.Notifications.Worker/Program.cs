using FCG.Notifications.Application.Configure;
using FCG.Notifications.Infra;
using FCG.Notifications.Infra.Configure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
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

using (var scope = host.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

await host.RunAsync();
