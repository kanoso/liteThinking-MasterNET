# Reto 2 - Guia de Implementacion

## Dominio Elegido: Sistema de Ordenes de Compra

Se implementa un sistema de ordenes donde un cliente puede **crear una orden**, **agregar items**, **listar todas las ordenes** y **obtener una orden por ID**. Esto cubre el flujo minimo exigido (crear entidad + agregar elemento) y ademas permite consultar la informacion almacenada.

---

## 1. Arquitectura: Clean Architecture / Capas

```mermaid
graph TB
    Api["<b>OrderSystem.Api</b><br/>Controllers, Program.cs, DI<br/>ASP.NET Core Web API"]
    App["<b>OrderSystem.Application</b><br/>Use Cases / Application Services<br/>CreateOrderUseCase, AddItemUseCase"]
    Domain["<b>OrderSystem.Domain</b><br/>Entidades, Value Objects, Interfaces<br/>Order, OrderItem, Money, IOrderRepository"]
    Infra["<b>OrderSystem.Infrastructure</b><br/>Implementacion de repos, persistencia<br/>InMemoryOrderRepository"]

    Api -->|depende de| App
    App -->|depende de| Domain
    Infra -.->|implementa interfaces de| Domain

    style Api fill:#4a90d9,stroke:#2c5f8a,color:#fff
    style App fill:#50b86c,stroke:#2d8a4a,color:#fff
    style Domain fill:#e8a838,stroke:#b07d1e,color:#fff
    style Infra fill:#9b59b6,stroke:#6c3483,color:#fff
```

### Diagrama de dependencias entre proyectos

```mermaid
graph LR
    Api((Api)) -->|ref| App((Application))
    App -->|ref| Domain((Domain))
    Infra((Infrastructure)) -->|ref| Domain
    Domain -.-x|NO depende de nada| Nada[ ]

    style Domain fill:#e8a838,stroke:#b07d1e,color:#fff,font-weight:bold
    style Nada fill:none,stroke:none
```

> El dominio es completamente independiente de frameworks y de infraestructura.

### Diagrama de Clases del Dominio

```mermaid
classDiagram
    class Order {
        -Guid Id
        -string CustomerName
        -OrderStatus Status
        -DateTime CreatedAt
        -List~OrderItem~ _items
        +IReadOnlyCollection~OrderItem~ Items
        +Create(customerName) Order$
        +AddItem(productName, quantity, unitPrice) void
        +CalculateTotal() Money
    }

    class OrderItem {
        -Guid Id
        -string ProductName
        -int Quantity
        -Money UnitPrice
        +GetSubtotal() Money
    }

    class Money {
        +decimal Amount
        +string Currency
        +Add(other) Money
        +Multiply(quantity) Money
        +Equals(other) bool
    }

    class OrderStatus {
        <<enumeration>>
        Pending
        Confirmed
        Cancelled
    }

    class IOrderRepository {
        <<interface>>
        +GetByIdAsync(id) Task~Order~
        +GetAllAsync() Task~IEnumerable~Order~~
        +AddAsync(order) Task
        +UpdateAsync(order) Task
    }

    class InMemoryOrderRepository {
        -Dictionary~Guid, Order~ _orders
        +GetByIdAsync(id) Task~Order~
        +GetAllAsync() Task~IEnumerable~Order~~
        +AddAsync(order) Task
        +UpdateAsync(order) Task
    }

    class CreateOrderUseCase {
        -IOrderRepository _orderRepository
        +ExecuteAsync(request) Task~OrderResponse~
    }

    class AddItemToOrderUseCase {
        -IOrderRepository _orderRepository
        +ExecuteAsync(orderId, request) Task~OrderResponse~
    }

    class GetOrdersUseCase {
        -IOrderRepository _orderRepository
        +ExecuteAsync() Task~IEnumerable~OrderResponse~~
        +ExecuteByIdAsync(id) Task~OrderResponse~
    }

    Order "1" *-- "*" OrderItem : contiene
    OrderItem --> Money : usa
    Order --> OrderStatus : tiene
    Order --> Money : calcula total
    InMemoryOrderRepository ..|> IOrderRepository : implementa
    CreateOrderUseCase --> IOrderRepository : usa
    AddItemToOrderUseCase --> IOrderRepository : usa
    CreateOrderUseCase --> Order : crea
    AddItemToOrderUseCase --> Order : modifica
    GetOrdersUseCase --> IOrderRepository : usa
```

---

## 2. Patrones de Diseno Utilizados

### 2.1 Repository Pattern

**Donde:** `IOrderRepository` (Domain) + `InMemoryOrderRepository` (Infrastructure)

**Justificacion:** Actua como frontera entre el dominio y la persistencia. El dominio define la interfaz (`IOrderRepository`) y la infraestructura la implementa. Esto permite cambiar la persistencia (de in-memory a SQL, MongoDB, etc.) sin tocar el dominio.

**Trade-off:** Agrega una capa de abstraccion. En un proyecto pequeno podria verse como over-engineering, pero aqui se justifica porque es requisito del reto y permite testear el dominio sin base de datos real.

### 2.2 Dependency Injection (DI)

**Donde:** `Program.cs` en la capa Api registra las dependencias.

**Justificacion:** Desacopla las capas. El controlador no sabe que repositorio concreto se usa; solo conoce la abstraccion. Esto facilita el testing y el intercambio de implementaciones.

### 2.3 Factory Method (condicional)

**Donde:** Metodo estatico `Order.Create(...)` dentro de la entidad `Order`.

**Justificacion:** La creacion de una Order tiene reglas de negocio (validar que el cliente no sea vacio, generar ID, establecer estado inicial). Encapsular esto en un factory method dentro de la propia entidad evita que la logica de creacion quede dispersa. Se usa **solo porque hay reglas**, no por obligacion.

**Trade-off:** Una clase Factory separada (`OrderFactory`) seria excesiva aqui porque no hay multiples variantes de Order. El factory method en la entidad es suficiente.

### 2.4 Value Object

**Donde:** `Money` (valor monetario con moneda).

**Justificacion:** Evita usar `decimal` suelto para representar precios. `Money` garantiza inmutabilidad, validacion (no negativo) y igualdad por valor. Esto es un concepto central de DDD.

### 2.5 Rich Domain Model (anti-modelo anemico)

**Donde:** La entidad `Order` contiene comportamiento (`AddItem`, `CalculateTotal`), no solo propiedades.

**Justificacion:** Es requisito del reto. Las reglas de negocio viven en el dominio, no en los servicios de aplicacion.

---

## 3. Estructura del Proyecto

```mermaid
graph LR
    subgraph "OrderSystem.sln"
        subgraph "src/OrderSystem.Domain"
            D_Entities["Entities/<br/>Order.cs<br/>OrderItem.cs"]
            D_VO["ValueObjects/<br/>Money.cs"]
            D_Enums["Enums/<br/>OrderStatus.cs"]
            D_Repos["Repositories/<br/>IOrderRepository.cs"]
        end
        subgraph "src/OrderSystem.Application"
            A_UC["UseCases/<br/>CreateOrderUseCase.cs<br/>AddItemToOrderUseCase.cs<br/>GetOrdersUseCase.cs"]
            A_DTO["DTOs/<br/>CreateOrderRequest.cs<br/>AddItemRequest.cs<br/>OrderResponse.cs"]
        end
        subgraph "src/OrderSystem.Infrastructure"
            I_Repos["Repositories/<br/>InMemoryOrderRepository.cs"]
        end
        subgraph "src/OrderSystem.Api"
            API_Ctrl["Controllers/<br/>OrdersController.cs"]
            API_Prog["Program.cs"]
        end
    end

    style D_Entities fill:#e8a838,color:#000
    style D_VO fill:#e8a838,color:#000
    style D_Enums fill:#e8a838,color:#000
    style D_Repos fill:#e8a838,color:#000
    style A_UC fill:#50b86c,color:#fff
    style A_DTO fill:#50b86c,color:#fff
    style I_Repos fill:#9b59b6,color:#fff
    style API_Ctrl fill:#4a90d9,color:#fff
    style API_Prog fill:#4a90d9,color:#fff
```

---

## 4. Flujo End-to-End del Caso de Uso

### Caso de Uso 1: Crear Orden

```mermaid
sequenceDiagram
    actor Cliente
    participant API as OrdersController
    participant UC as CreateOrderUseCase
    participant Entity as Order
    participant Repo as InMemoryOrderRepository

    Cliente->>API: POST /api/orders<br/>{ "customerName": "Juan Perez" }
    API->>UC: ExecuteAsync(request)
    UC->>Entity: Order.Create("Juan Perez")
    Note over Entity: Factory Method:<br/>Valida nombre<br/>Genera ID<br/>Estado = Pending
    Entity-->>UC: order
    UC->>Repo: AddAsync(order)
    Note over Repo: Persiste en Dictionary
    Repo-->>UC: completado
    UC-->>API: OrderResponse
    API-->>Cliente: 201 Created + OrderResponse
```

### Caso de Uso 2: Agregar Item a Orden

```mermaid
sequenceDiagram
    actor Cliente
    participant API as OrdersController
    participant UC as AddItemToOrderUseCase
    participant Repo as InMemoryOrderRepository
    participant Entity as Order
    participant VO as Money

    Cliente->>API: POST /api/orders/{id}/items<br/>{ "productName": "Laptop",<br/>"quantity": 2, "unitPrice": 999.99 }
    API->>UC: ExecuteAsync(orderId, request)
    UC->>Repo: GetByIdAsync(orderId)
    Repo-->>UC: order
    UC->>VO: new Money(999.99, "S/.")
    Note over VO: Value Object:<br/>Valida monto >= 0<br/>Inmutable
    VO-->>UC: unitPrice
    UC->>Entity: order.AddItem("Laptop", 2, unitPrice)
    Note over Entity: Crea OrderItem interno
    Entity-->>UC: void
    UC->>Repo: UpdateAsync(order)
    Repo-->>UC: completado
    UC-->>API: OrderResponse
    API-->>Cliente: 200 OK + OrderResponse<br/>(con items y total calculado)
```

### Caso de Uso 3: Listar Todas las Ordenes

```mermaid
sequenceDiagram
    actor Cliente
    participant API as OrdersController
    participant UC as GetOrdersUseCase
    participant Repo as InMemoryOrderRepository

    Cliente->>API: GET /api/orders
    API->>UC: ExecuteAsync()
    UC->>Repo: GetAllAsync()
    Repo-->>UC: Lista de Order
    Note over UC: Mapea cada Order<br/>a OrderResponse con<br/>items y total calculado
    UC-->>API: Lista de OrderResponse
    API-->>Cliente: 200 OK + lista de ordenes
```

### Caso de Uso 4: Obtener Orden por ID

```mermaid
sequenceDiagram
    actor Cliente
    participant API as OrdersController
    participant UC as GetOrdersUseCase
    participant Repo as InMemoryOrderRepository

    Cliente->>API: GET /api/orders/{id}
    API->>UC: ExecuteByIdAsync(id)
    UC->>Repo: GetByIdAsync(id)
    alt Orden encontrada
        Repo-->>UC: order
        Note over UC: Mapea a OrderResponse
        UC-->>API: OrderResponse
        API-->>Cliente: 200 OK + OrderResponse
    else Orden no encontrada
        Repo-->>UC: null
        UC-->>API: null
        API-->>Cliente: 404 Not Found
    end
```

---

## 5. Decisiones Arquitectonicas y Trade-offs

| Decision | Justificacion | Trade-off |
|----------|---------------|-----------|
| **In-Memory Repository** | Simplicidad. No se requiere persistencia real. | Los datos se pierden al reiniciar la app. Suficiente para el alcance. |
| **Factory Method en la entidad** (no clase separada) | Solo hay un tipo de Order. Una clase `OrderFactory` separada seria over-engineering. | Si en el futuro hay multiples tipos de ordenes, habria que refactorizar. |
| **Value Object `Money`** | Evita primitive obsession. Garantiza validacion e inmutabilidad. | Agrega una clase extra, pero el beneficio en claridad y seguridad lo justifica. |
| **Use Cases como clases separadas** (no un unico servicio) | Cada caso de uso tiene una responsabilidad unica (SRP). Facilita testing. | Mas archivos que un solo `OrderService`, pero mejor cohesion. |
| **DTOs en Application** | Evita exponer entidades del dominio a la capa de presentacion. | Requiere mapeo manual (sin AutoMapper para mantener simplicidad). |
| **No se usa CQRS** | El reto dice explicitamente que no se esperan patrones avanzados. | Para un sistema grande seria beneficioso separar lecturas de escrituras. |
| **Singleton para InMemoryRepository** | Necesario para que los datos persistan entre requests HTTP. | Con una DB real seria `Scoped`. |
| **Constructor `internal` en OrderItem** | Solo `Order` puede crear items, protegiendo la invariante del agregado. | Requiere que `Order` y `OrderItem` esten en el mismo proyecto/assembly. |

---

## 6. Principios SOLID Aplicados

| Principio | Como se aplica |
|-----------|----------------|
| **S** - Single Responsibility | Cada Use Case hace una sola cosa. La entidad gestiona sus propias reglas. |
| **O** - Open/Closed | Se pueden agregar nuevos Use Cases sin modificar los existentes. |
| **L** - Liskov Substitution | `InMemoryOrderRepository` es sustituible por cualquier `IOrderRepository`. |
| **I** - Interface Segregation | `IOrderRepository` solo tiene los metodos necesarios. |
| **D** - Dependency Inversion | Application depende de abstracciones (interfaces en Domain), no de implementaciones concretas. |

---

## 7. Conceptos DDD Aplicados

| Concepto | Implementacion |
|----------|----------------|
| **Entidad** | `Order` (tiene identidad unica por `Id` y ciclo de vida) |
| **Value Object** | `Money` (inmutable, igualdad por valor) |
| **Aggregate Root** | `Order` controla el acceso a sus `OrderItems` |
| **Repository** | `IOrderRepository` define el contrato en el dominio |
| **Ubiquitous Language** | Nombres como `Order`, `OrderItem`, `AddItem`, `CalculateTotal` reflejan el lenguaje del negocio |
| **Rich Model** | Los metodos `AddItem()`, `CalculateTotal()` contienen reglas de negocio |
