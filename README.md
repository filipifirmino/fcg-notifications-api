# FCG Notifications API — Grupo 14

Microsserviço responsável por simular o envio de e-mails da plataforma FIAP Cloud Games (FCG), operando exclusivamente via mensageria assíncrona (sem endpoints HTTP). Serviço novo criado na Fase 2 — migração para microsserviços.

---

## Sumário

- [Responsabilidade](#responsabilidade)
- [Arquitetura](#arquitetura)
- [Tecnologias](#tecnologias)
- [Fluxo de Mensagens](#fluxo-de-mensagens)
- [Notificações Simuladas](#notificações-simuladas)
- [Pré-requisitos](#pré-requisitos)
- [Variáveis de Ambiente](#variáveis-de-ambiente)
- [Rodando com Docker](#rodando-com-docker)
- [Rodando localmente](#rodando-localmente)
- [Estrutura do Projeto](#estrutura-do-projeto)
- [Logs](#logs)

---

## Responsabilidade

| Item | Detalhe |
|---|---|
| Domínio | Notifications |
| Tipo | Worker Service (sem HTTP endpoints) |
| Banco de dados | Nenhum (sem persistência) |
| Consome eventos | `UserCreatedEvent` e `PaymentProcessedEvent` ← RabbitMQ |
| Saída | Logs no console simulando envio de e-mail |

---

## Arquitetura

Serviço do tipo **Worker Service** — sem controllers ou endpoints HTTP. Toda comunicação ocorre via eventos RabbitMQ usando MassTransit. É o serviço mais simples da solução, sem camada de domínio ou banco de dados.

```
fcg-notifications-api/
└── src/
    └── FCG.Notifications.Worker/
        ├── Consumers/               # UserCreatedConsumer, PaymentProcessedConsumer
        ├── Services/                # NotificationService
        ├── Program.cs
        └── appsettings.json
```

### Padrões aplicados

- **Event-Driven** — reage exclusivamente a eventos; não expõe HTTP e não persiste dados
- **Consumer Pattern** — consumers independentes por tipo de evento
- **Simulação** — e-mails são logados no console; sem integração SMTP real

---

## Tecnologias

| Camada | Tecnologia | Versão |
|---|---|---|
| Runtime | .NET | 10.0 |
| Tipo de projeto | Worker Service | — |
| Mensageria | MassTransit + RabbitMQ | — |
| Logging | Serilog | 10.0.0 |

---

## Fluxo de Mensagens

```
UsersAPI
    │
    │  UserCreatedEvent  →  [user.created]
    │
    ▼
NotificationsAPI (UserCreatedConsumer)
    └── loga e-mail de boas-vindas

PaymentsAPI
    │
    │  PaymentProcessedEvent  →  [payment.processed]
    │
    ▼
NotificationsAPI (PaymentProcessedConsumer)
    ├── Status=Approved  → loga e-mail de confirmação de compra
    └── Status=Rejected  → (silencioso — nenhuma notificação enviada)
```

### UserCreatedEvent (consumido)

```csharp
public record UserCreatedEvent
{
    public Guid UserId { get; init; }
    public string Name { get; init; }
    public string Email { get; init; }
    public DateTime CreatedAt { get; init; }
}
```

### PaymentProcessedEvent (consumido)

```csharp
public record PaymentProcessedEvent
{
    public Guid OrderId { get; init; }
    public Guid UserId { get; init; }
    public Guid GameId { get; init; }
    public string GameTitle { get; init; }
    public string UserEmail { get; init; }
    public decimal Amount { get; init; }
    public PaymentStatus Status { get; init; }  // Approved | Rejected
    public string? Reason { get; init; }
    public DateTime ProcessedAt { get; init; }
}
```

---

## Notificações Simuladas

### E-mail de boas-vindas (UserCreatedEvent)

```
[EMAIL SIMULADO] Boas-vindas enviado para {Email}.
Olá {Name}, bem-vindo à FCG!
```

### E-mail de confirmação de compra (PaymentProcessedEvent — Approved)

```
[EMAIL SIMULADO] Confirmação de compra enviada para {UserEmail}.
Jogo: {GameTitle}, Valor: R$ {Amount:F2}
```

---

## Pré-requisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) — RabbitMQ via Docker Compose (repositório `fcg-infra`)

---

## Variáveis de Ambiente

| Variável | Descrição | Padrão (dev) |
|---|---|---|
| `RabbitMq__Host` | Host do RabbitMQ | `rabbitmq` |
| `RabbitMq__Username` | Usuário do RabbitMQ | `guest` |
| `RabbitMq__Password` | Senha do RabbitMQ | `guest` |

---

## Rodando com Docker

Suba toda a infraestrutura a partir do repositório `fcg-infra`:

```bash
docker compose up -d
```

O worker iniciará automaticamente e ficará aguardando eventos no RabbitMQ.

Acompanhe os logs em tempo real para ver as notificações simuladas:

```bash
docker logs -f fcg_notifications_worker
```

---

## Rodando localmente

### 1. Configure o RabbitMQ

```bash
docker run -d --name rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:3-management-alpine
```

### 2. Restaure dependências

```bash
dotnet restore
```

### 3. Execute o Worker

```bash
dotnet run --project src/FCG.Notifications.Worker/FCG.Notifications.Worker.csproj
```

---

## Estrutura do Projeto

```
src/
└── FCG.Notifications.Worker/
    ├── Consumers/
    │   ├── UserCreatedConsumer.cs          # Boas-vindas
    │   └── PaymentProcessedConsumer.cs     # Confirmação de compra
    ├── Services/
    │   └── NotificationService.cs          # Formata e loga as mensagens
    ├── Program.cs
    └── appsettings.json
```

---

## Logs

Todos os eventos processados são registrados via **Serilog**:

| Evento | Nível | Mensagem |
|---|---|---|
| Usuário criado | Information | `[EMAIL SIMULADO] Boas-vindas enviado para {Email}` |
| Compra aprovada | Information | `[EMAIL SIMULADO] Confirmação de compra enviada para {Email}` |
| Erro no consumer | Error | Stack trace completo |

---

## Grupo 14

Projeto desenvolvido para a disciplina **Full Stack Developer** — FIAP.
