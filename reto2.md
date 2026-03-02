## Reto 2. Arquitectura, Calidad de Código y Diseño en Capas

### Descripción del reto

En este reto práctico el estudiante deberá diseñar y construir una **base de proyecto en .NET** que refleje los principios de **calidad de código**, **arquitectura en capas**, **uso consciente de patrones de diseño** e **introducción práctica a DDD**, vistos durante las sesiones del **Módulo 2**.

El reto se realizará de forma **asíncrona** y tiene como propósito evaluar el **criterio técnico y arquitectónico** del estudiante, no la cantidad de funcionalidades implementadas.

### Objetivo

Demostrar la capacidad de:

·      Diseñar una solución con **arquitectura en capas clara y coherente**.

·      Definir un **dominio con comportamiento**, evitando modelos anémicos.

·      Implementar al menos un **caso de uso completo (end-to-end)**.

·      Aplicar patrones de diseño únicamente cuando aportan valor.

·      Justificar decisiones técnicas y **trade-offs** asumidos.

### Instrucciones

1. Crear una solución en .NET que implemente una arquitectura en capas.
2. Definir al menos **una entidad principal** dentro del dominio, con identidad y comportamiento.
3. Implementar **al menos un caso de uso funcional completo**, que recorra las capas:

**Presentation → Application → Domain → Repository → Infrastructure**

1. Aplicar Dependency Injection para el desacoplamiento de dependencias.
2. Implementar el patrón Repository como frontera entre el dominio y la persistencia.
3. Utilizar Factory únicamente si la creación de objetos implica reglas o decisiones.
4. Incluir una API mínima que permita demostrar el flujo completo del caso de uso.
5. Documentar las decisiones arquitectónicas tomadas.

### Alcance del reto

#### Flujo mínimo esperado

El proyecto debe incluir, como mínimo, un caso de uso funcional completo, por ejemplo:

·      Crear una entidad principal (ej. Order).

·      Agregar un elemento a dicha entidad.

No se aceptarán entregas que incluyan únicamente la estructura del proyecto sin un flujo funcional claro.

### Consideraciones importantes

#### Se espera

·      Separación clara de capas.

·      Dominio independiente de frameworks.

·      Entidades con reglas y comportamiento.

·      Uso consciente de patrones.

·      Código legible y mantenible.

#### No se espera

·      Sistema completo.

·      Múltiples funcionalidades.

·      Interfaz gráfica.

·      Seguridad, autenticación o autorización.

·      Persistencia real compleja.

·      Optimización de performance.

·      Microservicios.

·      CQRS, Event Sourcing u otros patrones avanzados.

### Arquitectura esperada

La solución deberá organizarse en proyectos separados, por ejemplo:

/YourProject.Domain

/YourProject.Application

/YourProject.Infrastructure

/YourProject.Api

#### Dependencias permitidas

·      Api → Application

·      Application → Domain

·      Infrastructure → Domain

El dominio **no debe depender** de frameworks ni de infraestructura.

### Entregables

El estudiante deberá entregar:

1.      **Repositorio del proyecto** con el código fuente.

2.      **Archivo README.md** que incluya:

o  Descripción general de la arquitectura.

o  Explicación de las capas.

o  Patrones utilizados y justificación.

o  Decisiones arquitectónicas relevantes.

Trade-offs asumidos.

Peso: 10%

Intentos: Ilimitados

Límite Marzo 22, 2026 23:59 h

Calificación: 0/100Reto
