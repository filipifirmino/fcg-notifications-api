using FCG.Notifications.Application.Configure;
using FCG.Notifications.Domain.Interfaces;
using FCG.Notifications.Infra.Consumers;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;

namespace FCG.Notifications.Tests.Integration.Config;

public class NotificationsTestFactory : IAsyncLifetime
{
    public IHost Host { get; private set; } = null!;
    public Mock<INotificationService> NotificationServiceMock { get; } = new();

    public async Task InitializeAsync()
    {
        NotificationServiceMock
            .Setup(s => s.SendWelcomeEmailAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        NotificationServiceMock
            .Setup(s => s.SendPurchaseConfirmationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<decimal>()))
            .Returns(Task.CompletedTask);
        NotificationServiceMock
            .Setup(s => s.SendPurchaseRejectedAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);

        Host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                // Register mock: overrides AddApplicationConfiguration's real NotificationService
                services.AddApplicationConfiguration();
                services.AddSingleton<INotificationService>(NotificationServiceMock.Object);

                services.AddMassTransitTestHarness(x =>
                {
                    x.AddConsumer<UserCreatedConsumer>();
                    x.AddConsumer<PaymentProcessedConsumer>();
                });
            })
            .Build();

        await Host.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await Host.StopAsync();
        Host.Dispose();
    }

    public ITestHarness GetTestHarness()
        => Host.Services.GetRequiredService<ITestHarness>();
}
