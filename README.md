# FCG Notifications API — Grupo 14

Microsserviço responsável por simular o envio de e-mails da plataforma FIAP Cloud Games (FCG), operando via mensageria assíncrona (sem endpoints HTTP) e registrando cada notificação simulada em banco de dados. Serviço criado na Fase 2 — migração para microsserviços.

---

## Sumário

- [Responsabilidade](#responsabilidade)
- [Arquitetura](#arquitetura)
- [Tecnologias](#tecnologias)
- [Fluxo de Mensagens](#fluxo-de-mensagens)
- [Notificações Simuladas](#notificações-simuladas)
- [Registro de Notificações (notification_logs)](#registro-de-notificações-notification_logs)
- [Testes](#testes)
- [Pré-requisitos](#pré-requisitos)
- [Variáveis de Ambiente](#variáveis-de-ambiente)
- [Rodando localmente](#rodando-localmente)
- [Rodando com Docker Compose](#rodando-com-docker-compose)
- [Kubernetes](#kubernetes)
- [Estrutura do Projeto](#estrutura-do-projeto)
- [Logs](#logs)

---

## Responsabilidade

| Item | Detalhe |
|---|---|
| Domínio | Notifications |
| Tipo | Worker Service (sem HTTP endpoints) |
| Banco de dados | PostgreSQL — `fcg_notifications_db` (tabela `notification_logs`) |
| Consome eventos | `UserCreatedEvent` e `PaymentProcessedEvent` ← RabbitMQ |
| Saída | Log estruturado (console) + registro persistido em banco simulando envio de e-mail |

---

## Arquitetura

Serviço do tipo **Worker Service** organizado em **Clean Architecture** com 4 camadas de código + 1 projeto de testes, seguindo o mesmo padrão dos demais microsserviços do ecossistema FCG.

```
Domain ← Application ← Infra ← Worker
                          ↑
                        Tests
```

### Camadas

| Projeto | Responsabilidade | Dependências externas |
|---|---|---|
| `FCG.Notifications.Domain` | Entidade `NotificationLog`, enum `NotificationType`, interfaces (`INotificationService`, `INotificationLogRepository`) | Nenhuma |
| `FCG.Notifications.Application` | Implementação do serviço de notificação (log + persistência) | Domain, Logging.Abstractions |
| `FCG.Notifications.Infra` | Consumers MassTransit, contratos de evento, `AppDbContext`/EF Core, repositório, configuração RabbitMQ | Application, MassTransit.RabbitMQ, Npgsql.EntityFrameworkCore.PostgreSQL |
| `FCG.Notifications.Worker` | Entry point — bootstrap do host, DI e migrations automáticas | Infra, Serilog |
| `FCG.Notifications.Tests` | Testes unitários, BDD (Gherkin) e integração | Todos os anteriores |

### Padrões aplicados

- **Clean Architecture** — dependências sempre apontando para o centro (Domain)
- **Event-Driven** — reage exclusivamente a eventos; não expõe HTTP
- **Consumer Pattern** — consumers independentes por tipo de evento (camada Infra), cada um com fila própria e exclusiva
- **Retry exponencial** — MassTransit retenta até 5 vezes com backoff (1 s → 30 s)
- **Simulação com auditoria** — e-mails são logados no console **e** persistidos em `notification_logs`; sem integração SMTP real

---

## Tecnologias

| Camada | Tecnologia | Versão |
|---|---|---|
| Runtime | .NET | 10.0 |
| Tipo de projeto | Worker Service | — |
| Mensageria | MassTransit + RabbitMQ | 8.1.3 |
| ORM | Entity Framework Core + Npgsql | 10.0.9 |
| Convenção de nomes | EFCore.NamingConventions (snake_case) | 10.0.1 |
| Banco de dados | PostgreSQL | 16 |
| Logging | Serilog | 8.x |
| Testes | xUnit + Moq + FluentAssertions + Bogus | 2.9.3 / 4.20 / 8.9 / 35.6 |
| BDD | Reqnroll (Gherkin) | 3.0.0 |
| Testes de integração | MassTransit TestHarness + EF Core InMemory | 8.1.3 |

---

## Fluxo de Mensagens

```
UsersAPI
    │
    │  UserCreatedEvent  →  exchange/fila "UserCreated"
    │
    ▼
NotificationsAPI (UserCreatedConsumer)
    └── loga e-mail de boas-vindas + grava NotificationLog(Welcome)

PaymentsAPI
    │
    │  PaymentProcessedEvent  →  exchange "PaymentProcessed"
    │
    ▼
NotificationsAPI (PaymentProcessedConsumer, fila própria "notifications-worker-payment-processed")
    ├── Status=Approved  → loga e-mail de confirmação + grava NotificationLog(PurchaseConfirmation)
    └── Status=Rejected  → loga e-mail de rejeição + grava NotificationLog(PurchaseRejected)
```

> **Importante — contrato de evento compartilhado:** as classes `UserCreatedEvent` e `PaymentProcessedEvent` usam `namespace FCG.Events;`, **idêntico em todos os microsserviços** que publicam ou consomem esses eventos (UsersAPI, CatalogAPI, PaymentsAPI, NotificationsAPI). O MassTransit identifica o tipo de uma mensagem no wire pelo namespace + nome do tipo .NET — se cada serviço usasse um namespace próprio para sua cópia do contrato, publisher e consumer não se reconheceriam como o mesmo evento, e a mensagem seria descartada silenciosamente (fila `_skipped`). Ao criar ou alterar um contrato de evento, mantenha o namespace `FCG.Events` em todos os repositórios envolvidos.

### UserCreatedEvent (consumido)

```csharp
namespace FCG.Events;

public record UserCreatedEvent
{
    public Guid UserId { get; init; }
    public string Name { get; init; }
    public string Email { get; init; }
    public DateTime CreatedAt { get; init; }
}
```

**Publisher:** UsersAPI | **Exchange/Fila:** `UserCreated`

### PaymentProcessedEvent (consumido)

```csharp
namespace FCG.Events;

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

**Publisher:** PaymentsAPI | **Exchange:** `PaymentProcessed` | **Fila:** `notifications-worker-payment-processed`

> A fila é nomeada explicitamente (via `ReceiveEndpoint`) em vez de deixar o MassTransit derivar o nome a partir da classe `PaymentProcessedConsumer` — o CatalogAPI também tem uma classe com esse mesmo nome consumindo o mesmo evento, e sem nomes de fila distintos os dois serviços cairiam na mesma fila física, competindo pela mensagem em vez de cada um receber sua própria cópia.

---

## Notificações Simuladas

### E-mail de boas-vindas (UserCreatedEvent)

```
[EMAIL SIMULADO] Boas-vindas enviado para {Email}.
Olá {Name}, bem-vindo à FCG!
```

### E-mail de confirmação de compra (PaymentProcessedEvent — Approved)

```
[EMAIL SIMULADO] Confirmação de compra enviada para {Email}.
Jogo: {GameTitle}, Valor: R$ {Amount:F2}
```

### E-mail de rejeição de compra (PaymentProcessedEvent — Rejected)

```
[EMAIL SIMULADO] Rejeição de compra enviada para {Email}.
Jogo: {GameTitle}. Motivo: {Reason}
```

---

## Registro de Notificações (`notification_logs`)

Além do log em console, cada notificação simulada é persistida no PostgreSQL — pensado para facilitar a demonstração do fluxo (não há SMTP nem gateway de pagamento real para "provar" visualmente que o evento foi processado).

### Tabela `notification_logs`

| Coluna | Tipo | Descrição |
|---|---|---|
| `id` | uuid | Identificador único |
| `type` | text | `Welcome`, `PurchaseConfirmation` ou `PurchaseRejected` |
| `recipient` | varchar(200) | E-mail do destinatário |
| `message` | varchar(1000) | Corpo da mensagem simulada |
| `sent_at` | timestamptz | Data/hora UTC do envio |

Índice em `sent_at` para consultas por período. As migrations rodam automaticamente no startup do Worker (`db.Database.MigrateAsync()`), criando o banco `fcg_notifications_db` e a tabela na primeira execução.

Consulta rápida via `psql`:

```bash
psql -U fcg -d fcg_notifications_db -c "SELECT type, recipient, message, sent_at FROM notification_logs ORDER BY sent_at DESC LIMIT 10;"
```

---

## Testes

O projeto conta com **42 testes automatizados** cobrindo todas as camadas:

```bash
dotnet test
```

### Pirâmide de testes

| Tipo | Quantidade | Ferramentas | O que cobre |
|---|---|---|---|
| Unitários — Application | 13 | xUnit + Moq + Bogus | `NotificationService`: os 3 métodos, nível de log, persistência via `INotificationLogRepository`, task completada |
| Unitários — Infra (Consumers) | 12 | xUnit + Moq + Bogus | `UserCreatedConsumer` e `PaymentProcessedConsumer`: roteamento, argumentos, isolamento entre métodos |
| Unitários — Infra (Repository) | 4 | xUnit + FluentAssertions + EF Core InMemory | `NotificationLogRepository`: `AddAsync`, `GetRecentAsync`, ordenação, limite |
| BDD / Gherkin | 7 | Reqnroll 3.0 + Moq | Cenários em linguagem natural: boas-vindas, confirmação, rejeição, isolamento |
| Integração | 5 | MassTransit TestHarness | Publicação de eventos no bus, consumo real pelos consumers, chamadas ao serviço |
| **Total** | **42** | | |

### Cobertura de cenários

| Cenário | Teste |
|---|---|
| E-mail de boas-vindas é enviado ao criar usuário | Unit + BDD + Integração |
| Nome e e-mail corretos passados ao serviço | Unit + BDD |
| Confirmação enviada quando pagamento aprovado | Unit + BDD + Integração |
| Rejeição enviada quando pagamento rejeitado | Unit + BDD + Integração |
| Confirmação NÃO é enviada quando rejeitado | Unit + BDD |
| Rejeição NÃO é enviada quando aprovado | Unit + BDD |
| Motivo nulo tratado corretamente | Unit |
| Notificação é persistida em `notification_logs` a cada envio | Unit |
| Repositório retorna os mais recentes primeiro, respeitando limite | Unit |

---

## Pré-requisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- [dotnet-ef](https://learn.microsoft.com/ef/core/cli/dotnet) (apenas para gerar migrations localmente)

```bash
dotnet tool install --global dotnet-ef
```

---

## Variáveis de Ambiente

| Variável | Descrição | Padrão (dev) |
|---|---|---|
| `ConnectionStrings__Postgres` | Connection string do PostgreSQL | `Host=localhost;Port=5432;Database=fcg_notifications_db;Username=fcg;Password=fcg_secret` |
| `RabbitMq__Host` | Host do RabbitMQ | `localhost` |
| `RabbitMq__Username` | Usuário do RabbitMQ | `guest` |
| `RabbitMq__Password` | Senha do RabbitMQ | `guest` |

---

## Rodando localmente

### 1. Suba o PostgreSQL e o RabbitMQ

```bash
docker run -d --name postgres -e POSTGRES_USER=fcg -e POSTGRES_PASSWORD=fcg_secret -e POSTGRES_DB=fcg_notifications_db -p 5432:5432 postgres:16-alpine
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

O Worker aplica as migrations automaticamente ao iniciar — nenhuma ação manual necessária. O painel do RabbitMQ fica disponível em `http://localhost:15672` (guest/guest).

### 4. (Opcional) Adicionar migrations manualmente

```bash
dotnet ef migrations add <NomeDaMigration> \
  --project src/FCG.Notifications.Infra \
  --startup-project src/FCG.Notifications.Infra
```

---

## Rodando com Docker Compose

Este repositório inclui um `docker-compose.yml` para execução local isolada (PostgreSQL + RabbitMQ + este worker).

```bash
docker compose up --build
```

Acompanhe os logs em tempo real:

```bash
docker logs -f fcg_notifications_worker
```

Para subir toda a plataforma (todos os 4 microsserviços), utilize o repositório `fcg-infra`:

```bash
# No repositório fcg-infra
docker compose up -d
```

---

## Kubernetes

Os manifests estão em `/k8s/`, e todos os recursos devem ser criados no namespace `fcg`:

```bash
kubectl apply -f k8s/00-namespace.yaml
kubectl apply -f k8s/
```

| Arquivo | Descrição |
|---|---|
| `00-namespace.yaml` | Cria o namespace `fcg` (aplicado primeiro automaticamente por ordem alfabética) |
| `configmap.yaml` | `Serilog__MinimumLevel__Default` e demais configs não sensíveis |
| `secret.yaml` | `RabbitMq__Password` e `ConnectionStrings__Postgres` (base64) |
| `deployment.yaml` | Deployment com resource limits |
| `service.yaml` | Service headless (sem porta exposta) |

Para atualizar o secret com valores customizados:

```bash
echo -n "sua_senha" | base64
# Substituir o valor em k8s/secret.yaml antes do apply
```

> Ao subir via `fcg-infra` (orquestração de todos os serviços), a connection string do Postgres e as credenciais de RabbitMQ comuns aos serviços ficam centralizadas em `k8s/shared/` — veja o README do `fcg-infra` para detalhes.

---

## Estrutura do Projeto

```
fcg-notifications-api/
├── src/
│   ├── FCG.Notifications.Domain/
│   │   ├── Entities/
│   │   │   └── NotificationLog.cs              # Entidade de auditoria (Id, Type, Recipient, Message, SentAt)
│   │   ├── Enums/
│   │   │   └── NotificationType.cs             # Welcome | PurchaseConfirmation | PurchaseRejected
│   │   └── Interfaces/
│   │       ├── INotificationService.cs         # Contrato de domínio
│   │       └── INotificationLogRepository.cs   # AddAsync / GetRecentAsync
│   │
│   ├── FCG.Notifications.Application/
│   │   ├── Services/
│   │   │   └── NotificationService.cs          # Loga + persiste cada notificação simulada
│   │   └── Configure/
│   │       └── ApplicationConfigure.cs         # Extension: AddApplicationConfiguration()
│   │
│   ├── FCG.Notifications.Infra/
│   │   ├── Consumers/
│   │   │   ├── UserCreatedConsumer.cs          # Boas-vindas ao novo usuário
│   │   │   └── PaymentProcessedConsumer.cs     # Confirmação ou rejeição de compra
│   │   ├── Events/
│   │   │   ├── UserCreatedEvent.cs             # namespace FCG.Events (compartilhado entre serviços)
│   │   │   └── PaymentProcessedEvent.cs        # namespace FCG.Events + PaymentStatus enum
│   │   ├── Repositories/
│   │   │   └── NotificationLogRepository.cs    # Implementação EF Core de INotificationLogRepository
│   │   ├── Migrations/                         # Histórico de migrations EF Core
│   │   ├── AppDbContext.cs                     # DbSet<NotificationLog>
│   │   ├── AppDbContextFactory.cs              # Design-time factory (dotnet ef)
│   │   └── Configure/
│   │       └── ConfigureInfra.cs               # Extension: AddInfrastructure() — DbContext, MassTransit, filas
│   │
│   ├── FCG.Notifications.Worker/
│   │   ├── Program.cs                          # Bootstrap do host, DI e MigrateAsync() automático
│   │   ├── appsettings.json
│   │   └── appsettings.Development.json
│   │
│   └── FCG.Notifications.Tests/
│       ├── Unit/
│       │   ├── Application/Services/           # NotificationServiceTests (13 testes)
│       │   └── Infra/
│       │       ├── Consumers/                  # UserCreated + PaymentProcessed (12 testes)
│       │       └── Repositories/                # NotificationLogRepositoryTests (4 testes)
│       ├── BDD/
│       │   ├── Features/                       # UserCreatedNotification.feature (3 cenários)
│       │   │                                   # PaymentProcessedNotification.feature (4 cenários)
│       │   └── StepDefinitions/                # Step definitions Reqnroll
│       └── Integration/
│           ├── Config/                         # NotificationsTestFactory (MassTransit Harness)
│           └── Consumers/                      # ConsumersIntegrationTests (5 testes)
│
├── k8s/                                        # Manifests Kubernetes (namespace fcg)
├── docker-compose.yml                          # Ambiente local (Postgres + RabbitMQ + worker)
└── Dockerfile                                  # Multi-stage, usuário não-root
```

---

## Logs

Todos os eventos processados são registrados via **Serilog** (console) e, adicionalmente, persistidos em `notification_logs`:

| Evento | Nível | Mensagem |
|---|---|---|
| Usuário criado | `Information` | `[EMAIL SIMULADO] Boas-vindas enviado para {Email}` |
| Compra aprovada | `Information` | `[EMAIL SIMULADO] Confirmação de compra enviada para {Email}` |
| Compra rejeitada | `Warning` | `[EMAIL SIMULADO] Rejeição de compra enviada para {Email}` |
| Erro no consumer | `Error` | Stack trace completo com contexto do evento |

---

## Grupo 14

Projeto desenvolvido para a disciplina **Full Stack Developer** — FIAP.
