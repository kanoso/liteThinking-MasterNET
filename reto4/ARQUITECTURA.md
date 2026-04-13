# Reto 4 — Evolución hacia Arquitectura de Microservicios Distribuidos

> [!NOTE]
> Este documento describe el estado actual del sistema (Reto 3) y la evolución propuesta hacia
> una arquitectura distribuida basada en microservicios con API Gateway, comunicación por eventos
> y procesamiento asíncrono con workers.

---

## 1. Estado Actual — Reto 3

### Estructura del Proyecto

El Reto 3 implementa un sistema de órdenes con **dos servicios** desplegados en Kubernetes:

| Servicio | Tecnología | Exposición |
|---|---|---|
| `OrderSystem.Api` | ASP.NET Core (.NET 10) | NodePort 30080 |
| `OrderSystem.Notifications` | ASP.NET Core (.NET 10) | ClusterIP interno |

### Arquitectura Interna del Servicio de Órdenes

El servicio principal sigue una **arquitectura en capas con DDD**:

```
OrderSystem.Api
├── Domain          → Entidades, Value Objects, Interfaces de Repositorio
├── Application     → Use Cases, DTOs
├── Infrastructure  → InMemoryOrderRepository
└── Api             → Controllers, Program.cs
```

### Diagrama de Componentes — Reto 3

```mermaid
graph TB
    subgraph Cliente["Cliente (Browser / HTTP Client)"]
        C([":30080 NodePort"])
    end

    subgraph K8S["Kubernetes — Namespace: ordersystem"]
        subgraph API["OrderSystem.Api — ClusterIP / NodePort"]
            OC[OrdersController]
            CU[CreateOrderUseCase]
            AIU[AddItemToOrderUseCase]
            GOU[GetOrdersUseCase]
            REPO[(InMemory<br/>Repository)]

            OC --> CU
            OC --> AIU
            OC --> GOU
            CU --> REPO
            AIU --> REPO
            GOU --> REPO
        end

        subgraph NOTIF["OrderSystem.Notifications — ClusterIP"]
            NC[NotificationsController<br/>POST /notifications/notify]
        end
    end

    C --> OC
    OC -- "HTTP POST (sync)<br/>fire-and-forget" --> NC
```

> [!WARNING]
> **Comunicación síncrona y acoplada.** El `OrdersController` llama directamente al servicio de
> notificaciones vía `HttpClient`. Si el servicio de notificaciones está caído, el log muestra una
> advertencia pero la orden igual se crea. Esto es un **acoplamiento temporal** disfrazado de
> resiliencia — si en algún momento la notificación fuera crítica, se perdería silenciosamente.

---

### Flujo de Creación de una Orden — Reto 3

```mermaid
sequenceDiagram
    actor Cliente
    participant CTRL as OrdersController<br/>[Api Layer]
    participant UC as CreateOrderUseCase<br/>[Application Layer]
    participant DOM as Order.Create()<br/>[Domain Layer]
    participant REPO as InMemoryRepository<br/>[Infrastructure Layer]
    participant HTTP as IHttpClientFactory<br/>[Api Layer]
    participant NOTIF as NotificationsController<br/>[Notifications.Api]

    Cliente->>CTRL: POST /api/orders<br/>{ customerName: "Juan" }

    Note over CTRL: ModelState validation (ASP.NET Core)

    CTRL->>UC: ExecuteAsync(CreateOrderRequest)

    UC->>DOM: Order.Create(customerName)
    Note over DOM: Valida que customerName no sea vacío<br/>Asigna Id = Guid.NewGuid()<br/>Status = Pending<br/>CreatedAt = UtcNow
    DOM-->>UC: Order (instancia nueva)

    UC->>REPO: AddAsync(order)
    Note over REPO: _orders[order.Id] = order<br/>Dictionary[Guid, Order] en memoria
    REPO-->>UC: Task.CompletedTask

    UC->>UC: MapToResponse(order)
    Note over UC: Proyecta Order → OrderResponse (DTO)<br/>Items vacíos, Total = 0
    UC-->>CTRL: OrderResponse { Id, CustomerName,<br/>Status="Pending", Items=[], Total=0 }

    CTRL->>HTTP: CreateClient("notifications")
    HTTP-->>CTRL: HttpClient<br/>BaseAddress = http://notifications-api-svc

    CTRL->>NOTIF: POST /notifications/notify<br/>{ orderId, customerName }

    alt Notificaciones disponible
        NOTIF->>NOTIF: LogInformation(orderId, customerName)
        NOTIF-->>CTRL: 200 OK { message, orderId }
    else Notificaciones caído o timeout
        NOTIF-->>CTRL: HttpRequestException / timeout
        CTRL->>CTRL: LogWarning("No se pudo notificar...")<br/>⚠️ Error silenciado — la orden ya fue guardada
    end

    CTRL-->>Cliente: 201 Created<br/>Location: /api/orders/{id}<br/>Body: OrderResponse
```

> [!WARNING]
> **Punto crítico del flujo:** la orden se persiste en memoria **antes** de llamar a notificaciones.
> Si el proceso se reinicia después de guardar pero antes de notificar, la orden existe pero
> la notificación se perdió para siempre. No hay garantía de entrega — ni reintentos ni DLQ.

---

### Infraestructura — Reto 3

```mermaid
graph LR
    subgraph DockerCompose["docker-compose.yml"]
        S1[ordersystem-api<br/>puerto 8080]
        S2[notifications-api<br/>puerto 8081]
        S1 -- "depends_on" --> S2
    end

    subgraph Kubernetes["k8s/"]
        NS[Namespace: ordersystem]
        CM[ConfigMap:<br/>ordersystem-config]
        D1[Deployment:<br/>ordersystem-api<br/>1 replica]
        D2[Deployment:<br/>notifications-api<br/>1 replica]
        SVC1[Service: NodePort<br/>:30080 → :8080]
        SVC2[Service: ClusterIP<br/>:80 → :8080]

        NS --> D1
        NS --> D2
        CM --> D1
        D1 --> SVC1
        D2 --> SVC2
        D1 -- "NotificationsUrl=<br/>http://notifications-api-svc" --> SVC2
    end
```

---

### Problemas Identificados en Reto 3

> [!CAUTION]
> Estos son los problemas que motivan la evolución al Reto 4.

| # | Problema | Impacto |
|---|---|---|
| 1 | **Comunicación síncrona entre servicios** | Acoplamiento temporal — si Notifications falla, el evento se pierde |
| 2 | **Storage en memoria** | Sin persistencia — datos se pierden al reiniciar el pod |
| 3 | **Sin API Gateway** | El cliente accede directamente al servicio — sin enrutamiento, auth centralizada ni rate limiting |
| 4 | **Sin mensajería / broker** | No hay cola de eventos — no se puede escalar el procesamiento asíncrono |
| 5 | **Sin workers** | No hay procesamiento en background ni tareas desacopladas |
| 6 | **Single replica sin estado compartido** | No se puede escalar horizontalmente con InMemoryRepository |

---

## 2. Evolución Propuesta — Reto 4

### Objetivo

Transformar el sistema en una **arquitectura distribuida de microservicios** con:

- **API Gateway** como único punto de entrada para los clientes
- **Comunicación por eventos** entre servicios (desacoplamiento asíncrono)
- **Message Broker** (RabbitMQ) como bus de eventos
- **Worker Service** que procesa eventos en background
- **Persistencia real** (reemplaza InMemoryRepository)

---

### Nuevos Componentes

| Componente | Rol | Tecnología |
|---|---|---|
| `ApiGateway` | Enruta, autentica y protege | YARP / Nginx |
| `OrderService` | Dominio de órdenes (evolución del actual) | ASP.NET Core |
| `NotificationWorker` | Procesa eventos `OrderCreated` | .NET Worker Service |
| `InventoryService` | Valida stock antes de confirmar orden | ASP.NET Core |
| `RabbitMQ` | Message broker — bus de eventos | RabbitMQ |
| `PostgreSQL` | Persistencia de órdenes | PostgreSQL |

---

### Diagrama de Arquitectura — Reto 4

```mermaid
graph TB
    subgraph Clientes["Clientes externos"]
        WEB([Web App])
        MOB([Mobile App])
        EXT([Terceros / API])
    end

    subgraph Gateway["API Gateway — puerto 80/443"]
        GW[YARP / Nginx<br/>Routing + Auth + Rate Limiting]
    end

    subgraph Servicios["Microservicios — Kubernetes Namespace: ordersystem"]
        subgraph OrderSvc["Order Service — :8080"]
            OC[OrdersController]
            CU[CreateOrderUseCase]
            PUB[EventPublisher<br/>IMessageBus]
            DB1[(PostgreSQL<br/>Orders DB)]

            OC --> CU
            CU --> PUB
            CU --> DB1
        end

        subgraph InvSvc["Inventory Service — :8082"]
            IC[InventoryController]
            INVDB[(PostgreSQL<br/>Inventory DB)]
            IC --> INVDB
        end

        subgraph NotifWorker["Notification Worker — background"]
            W[WorkerService<br/>IHostedService]
            NS[NotificationSender<br/>Email / SMS / Push]
            W --> NS
        end
    end

    subgraph Broker["Message Broker"]
        RMQ[RabbitMQ<br/>Exchanges + Queues]
    end

    WEB --> GW
    MOB --> GW
    EXT --> GW

    GW -- "/orders/*" --> OC
    GW -- "/inventory/*" --> IC

    OC -- "HTTP sync<br/>validar stock" --> IC

    PUB -- "Publish: OrderCreated" --> RMQ
    RMQ -- "Subscribe: order.created" --> W

    style RMQ fill:#ff9900,color:#000
    style GW fill:#0066cc,color:#fff
    style W fill:#009933,color:#fff
```

---

### Flujo de Creación de Orden — Reto 4

```mermaid
sequenceDiagram
    actor Cliente
    participant GW as API Gateway
    participant OS as Order Service
    participant INV as Inventory Service
    participant DB as PostgreSQL
    participant MQ as RabbitMQ
    participant NW as Notification Worker

    Cliente->>GW: POST /orders
    GW->>GW: Autenticar JWT / Rate Limit
    GW->>OS: POST /api/orders (forward)

    OS->>INV: GET /inventory/validate (sync HTTP)
    INV-->>OS: 200 OK — stock disponible

    OS->>DB: INSERT order (persist)
    OS->>MQ: Publish event: OrderCreated
    OS-->>GW: 201 Created
    GW-->>Cliente: 201 Created

    Note over MQ,NW: Procesamiento asíncrono
    MQ->>NW: Deliver message: OrderCreated
    NW->>NW: Procesar notificación
    NW->>NW: Enviar Email / Push / SMS
```

> [!IMPORTANT]
> La diferencia clave con el Reto 3 es que **la notificación ya no bloquea la respuesta al cliente**.
> El Order Service publica el evento en RabbitMQ y responde inmediatamente con `201 Created`.
> El Notification Worker procesa el evento de forma completamente independiente y asíncrona.

---

### Diagrama de Eventos — Bus de Mensajes

```mermaid
graph LR
    subgraph Producers["Productores de Eventos"]
        OS[Order Service]
        INV[Inventory Service]
    end

    subgraph RabbitMQ["RabbitMQ — Topic Exchange: ordersystem"]
        E1{Exchange:<br/>ordersystem.events}
        Q1[Queue:<br/>order.created]
        Q2[Queue:<br/>order.cancelled]
        Q3[Queue:<br/>inventory.updated]

        E1 -- "routing: order.created" --> Q1
        E1 -- "routing: order.cancelled" --> Q2
        E1 -- "routing: inventory.updated" --> Q3
    end

    subgraph Consumers["Consumidores / Workers"]
        NW[Notification Worker<br/>order.created]
        AW[Audit Worker<br/>order.*]
        IW[Inventory Worker<br/>inventory.updated]
    end

    OS -- "OrderCreated" --> E1
    OS -- "OrderCancelled" --> E1
    INV -- "InventoryUpdated" --> E1

    Q1 --> NW
    Q1 --> AW
    Q2 --> AW
    Q3 --> IW

    style E1 fill:#ff9900,color:#000
    style NW fill:#009933,color:#fff
    style AW fill:#009933,color:#fff
    style IW fill:#009933,color:#fff
```

---

### Infraestructura Kubernetes — Reto 4

```mermaid
graph TB
    subgraph Ingress["Ingress Controller"]
        ING[nginx-ingress<br/>ordersystem.local]
    end

    subgraph NS["Namespace: ordersystem"]
        subgraph Deployments["Deployments"]
            D_GW[API Gateway<br/>2 replicas]
            D_OS[Order Service<br/>2 replicas]
            D_INV[Inventory Service<br/>1 replica]
            D_NW[Notification Worker<br/>1 replica]
        end

        subgraph Stateful["StatefulSets"]
            SS_RMQ[RabbitMQ<br/>StatefulSet]
            SS_PG[PostgreSQL<br/>StatefulSet]
        end

        subgraph Config["Configuración"]
            CM[ConfigMaps]
            SEC[Secrets<br/>DB passwords<br/>JWT keys]
            PVC[PersistentVolumeClaims<br/>RabbitMQ data<br/>Postgres data]
        end
    end

    ING --> D_GW
    D_GW --> D_OS
    D_GW --> D_INV
    D_OS --> SS_PG
    D_OS --> SS_RMQ
    D_NW --> SS_RMQ
    SS_RMQ --> PVC
    SS_PG --> PVC
    CM --> D_GW
    CM --> D_OS
    SEC --> D_OS
    SEC --> SS_PG

    style SS_RMQ fill:#ff9900,color:#000
    style SS_PG fill:#336699,color:#fff
```

---

### Comparación Reto 3 vs Reto 4

```mermaid
graph LR
    subgraph Reto3["🔴 Reto 3 — Acoplado"]
        R3_C([Cliente]) --> R3_API[Order API]
        R3_API -- "HTTP sync<br/>acoplado" --> R3_N[Notifications API]
        R3_API --- R3_MEM[(In-Memory)]
    end

    subgraph Reto4["🟢 Reto 4 — Distribuido"]
        R4_C([Cliente]) --> R4_GW[API Gateway]
        R4_GW --> R4_OS[Order Service]
        R4_OS -- "async event" --> R4_MQ[RabbitMQ]
        R4_MQ --> R4_W[Notification Worker]
        R4_OS --- R4_DB[(PostgreSQL)]
    end
```

| Aspecto | Reto 3 | Reto 4 |
|---|---|---|
| **Punto de entrada** | Directo al servicio | API Gateway centralizado |
| **Comunicación** | HTTP síncrono | Eventos asíncronos (RabbitMQ) |
| **Storage** | In-Memory (volátil) | PostgreSQL (persistente) |
| **Notificaciones** | Acopladas al controller | Worker independiente |
| **Escalabilidad** | Sin estado compartido | Stateless + broker desacoplado |
| **Resiliencia** | Si notif falla → log y olvida | Si notif falla → mensaje queda en cola y se reintenta |
| **Orquestación** | K8s básico (1 replica) | K8s con StatefulSets + PVC |

---

## 3. Estructura de Carpetas — Reto 4

```
reto4/
├── src/
│   ├── OrderSystem.ApiGateway/         ← Nuevo: API Gateway (YARP)
│   ├── OrderSystem.Domain/             ← Igual que reto3 (shared kernel)
│   ├── OrderSystem.Application/        ← Evolución: agrega IMessageBus, eventos
│   ├── OrderSystem.Infrastructure/     ← Evolución: PostgreSQL + RabbitMQ publisher
│   ├── OrderSystem.Api/                ← Evolución: sin HttpClient a notifications
│   ├── OrderSystem.InventoryService/   ← Nuevo: valida stock
│   └── OrderSystem.NotificationWorker/ ← Evolución: de API a Worker Service
├── k8s/
│   ├── namespace.yaml
│   ├── configmap.yaml
│   ├── secrets.yaml                    ← Nuevo
│   ├── deployments/
│   │   ├── gateway.yaml
│   │   ├── order-service.yaml
│   │   ├── inventory-service.yaml
│   │   └── notification-worker.yaml
│   └── statefulsets/
│       ├── rabbitmq.yaml               ← Nuevo
│       └── postgresql.yaml             ← Nuevo
├── docker-compose.yml                  ← Evolución: 5 servicios
└── ARQUITECTURA.md                     ← Este archivo
```

> [!TIP]
> El siguiente paso es implementar cada componente nuevo del Reto 4. El orden sugerido es:
>
> 1. Migrar `InMemoryRepository` a **PostgreSQL** (cambio de infraestructura puro)
> 2. Agregar **RabbitMQ** y el `EventPublisher` en el Order Service
> 3. Convertir `OrderSystem.Notifications` en un **Worker Service** que consume eventos
> 4. Agregar el **API Gateway** con YARP
> 5. Crear el **Inventory Service** y conectar la validación de stock
> 6. Actualizar los manifiestos de **Kubernetes** con los nuevos StatefulSets
