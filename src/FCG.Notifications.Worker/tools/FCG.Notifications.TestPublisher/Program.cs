using FCG.Notifications.Infra.Events;
using MassTransit;

// Publisher de teste: publica os três cenários que o worker consome.
// Uso: dotnet run [host]   (host padrão = localhost)
var host = args.Length > 0 ? args[0] : "localhost";

var bus = Bus.Factory.CreateUsingRabbitMq(cfg =>
{
    cfg.Host(host, h =>
    {
        h.Username("guest");
        h.Password("guest");
    });
});

await bus.StartAsync();
Console.WriteLine($"Publisher conectado em rabbitmq://{host}/");

try
{
    var userId = Guid.NewGuid();

    // 1) Novo usuário → worker loga e-mail de boas-vindas
    await bus.Publish(new UserCreatedEvent
    {
        UserId = userId,
        Name = "Diego Monzine",
        Email = "diego@fcg.com",
        CreatedAt = DateTime.UtcNow
    });
    Console.WriteLine("→ UserCreatedEvent publicado (espera: e-mail de boas-vindas)");

    // 2) Pagamento aprovado → worker loga confirmação de compra
    await bus.Publish(new PaymentProcessedEvent
    {
        OrderId = Guid.NewGuid(),
        UserId = userId,
        GameId = Guid.NewGuid(),
        GameTitle = "The Witcher 3",
        UserEmail = "diego@fcg.com",
        Amount = 79.90m,
        Status = PaymentStatus.Approved,
        ProcessedAt = DateTime.UtcNow
    });
    Console.WriteLine("→ PaymentProcessedEvent [Approved] publicado (espera: confirmação de compra)");

    // 3) Pagamento rejeitado → worker fica silencioso (nenhuma notificação)
    await bus.Publish(new PaymentProcessedEvent
    {
        OrderId = Guid.NewGuid(),
        UserId = userId,
        GameId = Guid.NewGuid(),
        GameTitle = "Cyberpunk 2077",
        UserEmail = "diego@fcg.com",
        Amount = 199.90m,
        Status = PaymentStatus.Rejected,
        Reason = "Cartão recusado",
        ProcessedAt = DateTime.UtcNow
    });
    Console.WriteLine("→ PaymentProcessedEvent [Rejected] publicado (espera: nenhum e-mail)");

    Console.WriteLine("\nEventos publicados. Verifique os logs do worker.");
}
finally
{
    await bus.StopAsync();
}
