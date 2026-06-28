# FCG Notifications API — Grupo 14

Microsserviço responsável por simular o envio de e-mails da plataforma FIAP Cloud Games (FCG), operando exclusivamente via mensageria assíncrona (sem endpoints HTTP). Serviço criado na Fase 2 — migração para microsserviços.

---

## Sumário

- [Responsabilidade](#responsabilidade)
- [Arquitetura](#arquitetura)
- [Tecnologias](#tecnologias)
- [Fluxo de Mensagens](#fluxo-de-mensagens)
- [Notificações Simuladas](#notificações-simuladas)
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
| Banco de dados | Nenhum (sem persistência) |
| Consome eventos | `UserCreatedEvent` e `PaymentProcessedEvent` ← RabbitMQ |
| Saída | Logs no console simulando envio de e-mail |

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
| `FCG.Notifications.Domain` | Interfaces de domínio (`INotificationService`) | Nenhuma |
| `FCG.Notifications.Application` | Implementação do serviço de notificação | Domain, Logging.Abstractions |
| `FCG.Notifications.Infra` | Consumers MassTransit, contratos de evento, configuração RabbitMQ | Application, MassTransit.RabbitMQ |
| `FCG.Notifications.Worker` | Entry point — bootstrap do host e DI | Infra, Serilog |
| `FCG.Notifications.Tests` | Testes unitários, BDD (Gherkin) e integração | Todos os anteriores |

### Padrões aplicados

- **Clean Architecture** — dependências sempre apontando para o centro (Domain)
- **Event-Driven** — reage exclusivamente a eventos; não expõe HTTP
- **Consumer Pattern** — consumers independentes por tipo de evento (camada Infra)
- **Retry exponencial** — MassTransit retenta até 5 vezes com backoff (1 s → 30 s)
- **Simulação** — e-mails são logados no console; sem integração SMTP real

---

## Tecnologias

| Camada | Tecnologia | Versão |
|---|---|---|
| Runtime | .NET | 10.0 |
| Tipo de projeto | Worker Service | — |
| Mensageria | MassTransit + RabbitMQ | 8.1.3 |
| Logging | Serilog | 8.x |
| Testes | xUnit + Moq + FluentAssertions | 2.9.3 / 4.20 / 8.9 |
| BDD | Reqnroll (Gherkin) | 3.0.0 |
| Testes de integração | MassTransit TestHarness | 8.1.3 |

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
    └── Status=Rejected  → loga e-mail de rejeição de compra
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

**Publisher:** UsersAPI | **Queue/Exchange:** `user.created`

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

**Publisher:** PaymentsAPI | **Queue/Exchange:** `payment.processed`

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

## Testes

O projeto conta com **35 testes automatizados** cobrindo todas as camadas:

```bash
dotnet test
```

### Pirâmide de testes

| Tipo | Quantidade | Ferramentas | O que cobre |
|---|---|---|---|
| Unitários — Application | 10 | xUnit + Moq + Bogus | `NotificationService`: todos os 3 métodos, nível de log, task completada |
| Unitários — Infra | 12 | xUnit + Moq + Bogus | `UserCreatedConsumer` e `PaymentProcessedConsumer`: roteamento, argumentos, isolamento entre métodos |
| BDD / Gherkin | 7 | Reqnroll 3.0 + Moq | Cenários em linguagem natural: boas-vindas, confirmação, rejeição, isolamento |
| Integração | 5 | MassTransit TestHarness | Publicação de eventos no bus, consumo real pelos consumers, chamadas ao serviço |
| **Total** | **35** | | |

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

---

## Pré-requisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)

---

## Variáveis de Ambiente

| Variável | Descrição | Padrão (dev) |
|---|---|---|
| `RabbitMq__Host` | Host do RabbitMQ | `localhost` |
| `RabbitMq__Username` | Usuário do RabbitMQ | `guest` |
| `RabbitMq__Password` | Senha do RabbitMQ | `guest` |

---

## Rodando localmente

### 1. Suba o RabbitMQ

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

O painel do RabbitMQ fica disponível em `http://localhost:15672` (guest/guest).

---

## Rodando com Docker Compose

Este repositório inclui um `docker-compose.yml` para execução local isolada (apenas este worker + RabbitMQ).

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

Os manifests estão em `/k8s/`:

```bash
kubectl apply -f k8s/
```

| Arquivo | Descrição |
|---|---|
| `configmap.yaml` | `RabbitMq__Host`, `RabbitMq__Username` |
| `secret.yaml` | `RabbitMq__Password` (base64) |
| `deployment.yaml` | Deployment com resource limits |
| `service.yaml` | Service headless (sem porta exposta) |

Para atualizar o secret com senha customizada:

```bash
echo -n "sua_senha" | base64
# Substituir o valor em k8s/secret.yaml antes do apply
```

---

## Estrutura do Projeto

```
fcg-notifications-api/
├── src/
│   ├── FCG.Notifications.Domain/
│   │   └── Interfaces/
│   │       └── INotificationService.cs         # Contrato de domínio
│   │
│   ├── FCG.Notifications.Application/
│   │   ├── Services/
│   │   │   └── NotificationService.cs          # Implementação via log estruturado
│   │   └── Configure/
│   │       └── ApplicationConfigure.cs         # Extension: AddApplicationConfiguration()
│   │
│   ├── FCG.Notifications.Infra/
│   │   ├── Consumers/
│   │   │   ├── UserCreatedConsumer.cs          # Boas-vindas ao novo usuário
│   │   │   └── PaymentProcessedConsumer.cs     # Confirmação ou rejeição de compra
│   │   ├── Events/
│   │   │   ├── UserCreatedEvent.cs             # Contrato do evento (cópia por valor)
│   │   │   └── PaymentProcessedEvent.cs        # Contrato do evento + PaymentStatus enum
│   │   └── Configure/
│   │       └── ConfigureInfra.cs               # Extension: AddInfrastructure()
│   │
│   ├── FCG.Notifications.Worker/
│   │   ├── Program.cs                          # Bootstrap do host e registro de DI
│   │   ├── appsettings.json
│   │   └── appsettings.Development.json
│   │
│   └── FCG.Notifications.Tests/
│       ├── Unit/
│       │   ├── Application/Services/           # NotificationServiceTests (10 testes)
│       │   └── Infra/Consumers/                # UserCreated + PaymentProcessed (12 testes)
│       ├── BDD/
│       │   ├── Features/                       # UserCreatedNotification.feature (3 cenários)
│       │   │                                   # PaymentProcessedNotification.feature (4 cenários)
│       │   └── StepDefinitions/                # Step definitions Reqnroll
│       └── Integration/
│           ├── Config/                         # NotificationsTestFactory (MassTransit Harness)
│           └── Consumers/                      # ConsumersIntegrationTests (5 testes)
│
├── k8s/                                        # Manifests Kubernetes
├── docker-compose.yml                          # Ambiente local (worker + RabbitMQ)
└── Dockerfile                                  # Multi-stage, usuário não-root
```

---

## Logs

Todos os eventos processados são registrados via **Serilog** (console + arquivo rotacionado diariamente em `logs/`):

| Evento | Nível | Mensagem |
|---|---|---|
| Usuário criado | `Information` | `[EMAIL SIMULADO] Boas-vindas enviado para {Email}` |
| Compra aprovada | `Information` | `[EMAIL SIMULADO] Confirmação de compra enviada para {Email}` |
| Compra rejeitada | `Warning` | `[EMAIL SIMULADO] Rejeição de compra enviada para {Email}` |
| Erro no consumer | `Error` | Stack trace completo com contexto do evento |

---

## Grupo 14

Projeto desenvolvido para a disciplina **Full Stack Developer** — FIAP.
