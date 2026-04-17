# Reto 4 — Evolución hacia Arquitectura Distribuida

## Índice

1. [Estado actual — Reto 3](#estado-actual--reto-3)
2. [Problemas identificados en Reto 3](#problemas-identificados-en-reto-3)
3. [Evolución — Reto 4](#evolución--reto-4)
4. [Flujo completo de una orden](#flujo-completo-de-una-orden)
5. [Contratos de eventos](#contratos-de-eventos)
6. [Estructura de proyectos](#estructura-de-proyectos)

---

## Estado actual — Reto 3

El reto 3 introduce un segundo servicio (`OrderSystem.Notifications`) y comunicación HTTP directa entre servicios. Es el primer paso hacia la distribución, pero mantiene acoplamiento temporal fuerte.

### Arquitectura de servicios

```mermaid
flowchart TD
    Client(["HTTP Client<br/>Postman / Browser"])

    subgraph OrderApi["OrderSystem.Api — :5268"]
        direction TB
        Controller["OrdersController<br/>POST /api/orders<br/>POST /api/orders/{id}/items<br/>GET  /api/orders<br/>GET  /health"]
        UC["Application Layer<br/>CreateOrderUseCase<br/>AddItemToOrderUseCase<br/>GetOrdersUseCase"]
        Repo["InMemoryOrderRepository<br/>Dictionary&lt;Guid, Order&gt;"]
    end

    subgraph Domain["OrderSystem.Domain"]
        direction LR
        OrderEntity["Order<br/>(Aggregate Root)"]
        ItemEntity["OrderItem<br/>(Entity)"]
        MoneyVO["Money<br/>(Value Object)"]
    end

    subgraph NotifApi["OrderSystem.Notifications — :55208"]
        NotifCtrl["NotificationsController<br/>POST /notifications/notify"]
        Logger["ILogger<br/>LogInformation(...)"]
    end

    Client -->|"HTTP POST /api/orders"| Controller
    Controller -->|"ExecuteAsync()"| UC
    UC --> Repo
    UC --> Domain
    Controller -->|"❗ HTTP síncrono<br/>IHttpClientFactory"| NotifCtrl
    NotifCtrl --> Logger

    classDef blue fill:#0984e3,stroke:#0984e3,color:#fff
    classDef green fill:#00b894,stroke:#00b894,color:#fff
    classDef yellow fill:#fdcb6e,stroke:#e17055,color:#2d3436
    classDef red fill:#d63031,stroke:#d63031,color:#fff

    class Controller,UC,Repo blue
    class OrderEntity,ItemEntity,MoneyVO green
    class NotifCtrl,Logger red
    class Client yellow
```

### Flujo de creación de una orden — Reto 3

```mermaid
sequenceDiagram
    autonumber
    actor Client as Cliente
    participant API as OrdersController
    participant UC  as CreateOrderUseCase
    participant Repo as InMemoryRepo
    participant NS  as Notifications :55208

    Client->>API: POST /api/orders { customerName }

    rect rgb(30, 100, 200)
        note over API,Repo: Lógica de negocio correcta
        API->>UC: ExecuteAsync(request)
        UC->>Repo: AddAsync(order)
        Repo-->>UC: order guardada
        UC-->>API: OrderResponse
    end

    rect rgb(200, 50, 50)
        note over API,NS: ❌ Problema — integración en el Controller
        API->>NS: POST /notifications/notify<br/>{ orderId, customerName }
        alt Notifications responde OK
            NS-->>API: 200 OK
        else Notifications caída / timeout
            NS--xAPI: HttpRequestException capturada
            note over API: LogWarning y continúa
        end
    end

    API-->>Client: 201 Created { id, status... }
    note over Client,NS: El cliente espera TODO el ciclo,<br/>incluyendo la llamada a Notifications
```

---

## Problemas identificados en Reto 3

#### ❌ Problema 1 — Responsabilidad mal ubicada

La llamada al servicio de Notifications está en el `OrdersController` (`OrdersController.cs:52-65`). El Controller tiene una sola responsabilidad: traducir HTTP a Use Cases y devolver una respuesta. La integración con otros servicios no le corresponde — eso es trabajo del Use Case o de la capa de Infrastructure.

#### ❌ Problema 2 — Acoplamiento temporal

La comunicación es HTTP síncrona: el cliente queda bloqueado esperando que Notifications responda antes de recibir su `201 Created`. Si Notifications es lento, el cliente espera más. Si Notifications cae, el error hay que capturarlo en el Controller con un `try/catch`. Dos servicios independientes terminan acoplados en tiempo de respuesta.

#### ❌ Problema 3 — Sin punto de entrada único

El cliente necesita conocer el puerto de cada servicio directamente (`Order API: :5268`, `Notifications: :55208`). Si un servicio cambia de puerto, se escala horizontalmente o se mueve a otro host, el cliente se rompe. No hay ninguna capa que abstraiga esa complejidad.

#### ❌ Problema 4 — Configuración faltante

`NotificationsUrl` no está declarada en `appsettings.json`. El `IHttpClientFactory` intenta leer esa key en runtime y falla silenciosamente o lanza una excepción según el contexto.

---

## Evolución — Reto 4

### Arquitectura objetivo

```mermaid
flowchart TD
    Client(["HTTP Client<br/>Postman / Browser"])

    subgraph GW["OrderSystem.Gateway — :5000"]
        YARP["YARP Reverse Proxy<br/>/api/orders/**  → :5001<br/>/notifications/** → :5002"]
    end

    subgraph OrderSvc["Order Service — :5001"]
        direction TB
        Ctrl["OrdersController<br/>solo HTTP → UseCase → response"]
        UseCase["CreateOrderUseCase<br/>guarda orden + publica evento"]
        InfraRepo["InMemoryOrderRepository"]
    end

    subgraph Messaging["Event Bus — Channel&lt;T&gt;"]
        direction LR
        IBus["IEventBus<br/>(abstracción)"]
        ImplBus["InMemoryEventBus<br/>Channel&lt;object&gt;"]
        IBus -.->|"implementa"| ImplBus
    end

    subgraph NotifSvc["Notification Service — :5002"]
        direction TB
        Worker["OrderCreatedWorker<br/>BackgroundService<br/>ReadAllAsync(cancellationToken)"]
        NotifCtrl2["NotificationsController<br/>POST /notifications/notify"]
        LogSvc["ILogger<br/>LogInformation(...)"]
        Worker --> LogSvc
        Worker -->|"opcional"| NotifCtrl2
    end

    Client -->|"HTTP :5000"| YARP
    YARP -->|"proxy /api/orders/**"| Ctrl
    YARP -->|"proxy /notifications/**"| NotifCtrl2
    Ctrl --> UseCase
    UseCase --> InfraRepo
    UseCase -->|"PublishAsync<br/>(OrderCreatedEvent)"| IBus
    ImplBus -->|"ReadAllAsync<br/>(asíncrono)"| Worker

    classDef purple fill:#6c5ce7,stroke:#6c5ce7,color:#fff
    classDef blue fill:#0984e3,stroke:#0984e3,color:#fff
    classDef teal fill:#00cec9,stroke:#00cec9,color:#fff
    classDef green fill:#00b894,stroke:#00b894,color:#fff
    classDef yellow fill:#fdcb6e,stroke:#e17055,color:#2d3436

    class YARP purple
    class Ctrl,UseCase,InfraRepo blue
    class IBus,ImplBus teal
    class Worker,NotifCtrl2,LogSvc green
    class Client yellow
```

### Componentes — detalle técnico

#### 1. API Gateway (YARP)

```mermaid
flowchart LR
    Client(["Cliente"])

    subgraph GW["API Gateway — YARP"]
        Router{{"Router<br/>appsettings.json"}}
    end

    subgraph Backends["Servicios internos"]
        OS["Order Service<br/>:5001"]
        NS["Notification Service<br/>:5002"]
    end

    Client -->|"POST /api/orders"| Router
    Client -->|"GET /api/orders"| Router
    Client -->|"POST /notifications/notify"| Router
    Router -->|"/api/orders/**"| OS
    Router -->|"/notifications/**"| NS

    classDef purple fill:#6c5ce7,stroke:#6c5ce7,color:#fff
    classDef blue fill:#0984e3,stroke:#0984e3,color:#fff
    classDef yellow fill:#fdcb6e,stroke:#e17055,color:#2d3436

    class Router purple
    class OS,NS blue
    class Client yellow
```

#### 2. Event Bus — flujo de publicación y consumo

```mermaid
flowchart LR
    subgraph Producer["Productor"]
        UC2["CreateOrderUseCase"]
    end

    subgraph Bus["Event Bus — Channel&lt;T&gt;"]
        Writer["ChannelWriter<br/>TryWrite(@event)"]
        Buffer[["buffer unbounded"]]
        Reader["ChannelReader<br/>ReadAllAsync(ct)"]
        Writer --> Buffer --> Reader
    end

    subgraph Consumer["Consumidor"]
        W["OrderCreatedWorker<br/>BackgroundService"]
    end

    UC2 -->|"PublishAsync<br/>(OrderCreatedEvent)"| Writer
    Reader -->|"IAsyncEnumerable&lt;object&gt;"| W

    classDef blue fill:#0984e3,stroke:#0984e3,color:#fff
    classDef teal fill:#00cec9,stroke:#00cec9,color:#fff
    classDef green fill:#00b894,stroke:#00b894,color:#fff

    class UC2 blue
    class Writer,Buffer,Reader teal
    class W green
```

#### 3. Evolución posible del Event Bus

```mermaid
flowchart LR
    IEB["«interface»<br/>IEventBus<br/>PublishAsync&lt;T&gt;<br/>ReadAllAsync"]

    Impl1["Fase 1<br/>InMemoryEventBus<br/>Channel&lt;T&gt;<br/>in-process"]
    Impl2["Fase 2<br/>RedisPubSubBus<br/>Redis Pub/Sub<br/>cross-process"]
    Impl3["Fase 3<br/>RabbitMqBus<br/>RabbitMQ / Azure SB<br/>cross-host"]

    IEB -->|"implementa"| Impl1
    IEB -->|"implementa"| Impl2
    IEB -->|"implementa"| Impl3

    classDef purple fill:#6c5ce7,stroke:#6c5ce7,color:#fff
    classDef green fill:#00b894,stroke:#00b894,color:#fff
    classDef blue fill:#0984e3,stroke:#0984e3,color:#fff
    classDef orange fill:#e17055,stroke:#e17055,color:#fff

    class IEB purple
    class Impl1 green
    class Impl2 blue
    class Impl3 orange
```

---

## Flujo completo de una orden

```mermaid
sequenceDiagram
    autonumber
    actor Client as Cliente
    participant GW  as API Gateway :5000
    participant OS  as Order Service :5001
    participant UC  as CreateOrderUseCase
    participant Bus as Event Bus
    participant W   as OrderCreatedWorker
    participant NS  as Notification Service :5002

    Client->>GW: POST /api/orders { customerName }
    GW->>OS: proxy → POST /api/orders

    rect rgb(30, 100, 200)
        note over OS,Bus: Request principal — síncrono y rápido
        OS->>UC: ExecuteAsync(request)
        UC->>UC: Order.Create(customerName)
        UC->>UC: repository.AddAsync(order)
        UC->>Bus: PublishAsync(OrderCreatedEvent)
        Bus-->>UC: Task.CompletedTask — no bloquea
        UC-->>OS: OrderResponse
    end

    OS-->>GW: 201 Created { id, status, total... }
    GW-->>Client: 201 Created { id, status, total... }

    note over Client,GW: El cliente recibe su respuesta<br/>sin esperar el procesamiento asíncrono

    rect rgb(0, 180, 148)
        note over Bus,NS: Procesamiento asíncrono — en background
        Bus-->>W: OrderCreatedEvent
        W->>W: LogInformation(orderId, customer, total)

        opt Notificación externa
            W->>NS: POST /notifications/notify
            NS-->>W: 200 OK
        end
    end
```

---

## Contratos de eventos

```mermaid
classDiagram
    class OrderCreatedEvent {
        +Guid OrderId
        +string CustomerName
        +DateTime CreatedAt
        +decimal Total
        +string Currency
    }

    class CreateOrderUseCase {
        -IOrderRepository repo
        -IEventBus eventBus
        +ExecuteAsync(request) OrderResponse
    }

    class OrderCreatedWorker {
        -IEventBus eventBus
        -ILogger logger
        +ExecuteAsync(ct)
    }

    class IEventBus {
        <<interface>>
        +PublishAsync(event)
        +ReadAllAsync(ct) IAsyncEnumerable
    }

    CreateOrderUseCase ..> IEventBus : publica
    CreateOrderUseCase ..> OrderCreatedEvent : crea
    OrderCreatedWorker ..> IEventBus : consume
    OrderCreatedWorker ..> OrderCreatedEvent : procesa
```

---

## Estructura de proyectos

```mermaid
flowchart TD
    subgraph reto4["reto4/src/"]
        GWProj["OrderSystem.Gateway 🆕<br/>Program.cs<br/>appsettings.json — rutas YARP"]
        ApiProj["OrderSystem.Api<br/>Controllers/OrdersController.cs<br/>Program.cs — registra IEventBus"]
        AppProj["OrderSystem.Application<br/>UseCases/CreateOrderUseCase.cs<br/>Events/OrderCreatedEvent.cs 🆕"]
        DomainProj["OrderSystem.Domain<br/>Entities / ValueObjects / Enums<br/>sin cambios"]
        InfraProj["OrderSystem.Infrastructure<br/>Repositories/InMemoryOrderRepository.cs<br/>Messaging/IEventBus.cs 🆕<br/>Messaging/InMemoryEventBus.cs 🆕"]
        NotifProj["OrderSystem.Notifications<br/>Controllers/NotificationsController.cs<br/>Workers/OrderCreatedWorker.cs 🆕<br/>Program.cs — registra Worker"]
    end

    GWProj -->|"proxy"| ApiProj
    GWProj -->|"proxy"| NotifProj
    ApiProj --> AppProj
    AppProj --> DomainProj
    AppProj --> InfraProj
    NotifProj --> InfraProj

    classDef purple fill:#6c5ce7,stroke:#6c5ce7,color:#fff
    classDef blue fill:#0984e3,stroke:#0984e3,color:#fff
    classDef teal fill:#00cec9,stroke:#00cec9,color:#fff
    classDef green fill:#00b894,stroke:#00b894,color:#fff
    classDef gray fill:#636e72,stroke:#636e72,color:#fff

    class GWProj purple
    class ApiProj,AppProj blue
    class InfraProj teal
    class NotifProj green
    class DomainProj gray
```

### Comparativa reto 3 vs reto 4

```mermaid
flowchart LR
    subgraph R3["Reto 3"]
        direction TB
        A["Cliente conoce<br/>cada puerto"]
        B["Controller llama<br/>HTTP síncrono"]
        C["Cliente espera<br/>a Notifications"]
        D["Sin Event Bus"]
        E["Sin Worker"]
    end

    subgraph R4["Reto 4"]
        direction TB
        F["Cliente solo conoce<br/>el Gateway :5000"]
        G["Use Case publica<br/>eventos asíncronos"]
        H["Cliente recibe 201<br/>antes del procesamiento"]
        I["Channel&lt;T&gt; Event Bus"]
        J["BackgroundService<br/>Worker independiente"]
    end

    A -->|"resuelto"| F
    B -->|"resuelto"| G
    C -->|"resuelto"| H
    D -->|"resuelto"| I
    E -->|"resuelto"| J

    classDef red fill:#d63031,stroke:#d63031,color:#fff
    classDef green fill:#00b894,stroke:#00b894,color:#fff

    class A,B,C,D,E red
    class F,G,H,I,J green
```

---

> **Nota de diseño:** El `InMemoryEventBus` con `Channel<T>` es suficiente para demostrar el patrón en desarrollo local. Pasar a RabbitMQ o Azure Service Bus en producción implica solo una nueva implementación de `IEventBus` y cambiar el registro en DI — los Use Cases y Workers no se tocan.
