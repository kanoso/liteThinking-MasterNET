## Reto 3. Contenerizacion y Orquestacion con Kubernetes

### Descripcion del reto

En este reto el estudiante debera evolucionar el proyecto del Modulo 2 hacia un entorno distribuido, aplicando Docker y orquestacion con Kubernetes.

### Objetivo

Demostrar la capacidad de contenerizar una aplicacion .NET, configurar comunicacion entre servicios, desplegar en Kubernetes, escalar replicas y evidenciar self-healing.

### Instrucciones

1. Crear un Dockerfile multi-stage funcional.
2. Construir la imagen del servicio.
3. Crear docker-compose.yml con al menos dos servicios.
4. Demostrar comunicacion entre contenedores.
5. Crear deployment.yaml y service.yaml.
6. Desplegar en Kubernetes local.
7. Escalar a 3 replicas.
8. Eliminar un Pod y evidenciar recreacion automatica.

### Alcance minimo esperado

Servicio desplegado en Kubernetes, expuesto mediante NodePort, con escalado y self-healing demostrados.

### Entregables

1. Repositorio actualizado con el codigo fuente.
2. Archivos YAML (docker-compose, manifiestos de Kubernetes).
3. Evidencias: logs o capturas de self-healing y escalado.
4. README explicativo que incluya:
   - Descripcion de la arquitectura de contenedores.
   - Decisiones arquitectonicas tomadas.
   - Trade-offs asumidos.

Peso: 10%

Intentos: Ilimitados

Limite: Marzo 22, 2026 23:59 h

---

## Implementacion

### Servicios

El sistema se compone de dos servicios que se comunican por nombre dentro de la red Docker interna:

**ordersystem-api** — API principal de ordenes. Al crear una orden llama al servicio de notificaciones via HTTP.

**notifications-api** — Minimal API con un unico endpoint `POST /notify`. Recibe el aviso de nueva orden y lo loguea. Sin persistencia ni logica adicional.

```
POST /api/orders  →  ordersystem-api  →  http://notifications-api/notify
```

La comunicacion usa el nombre de servicio Docker como host. No se necesita conocer IPs.

### Estructura de archivos

```
reto3/
├── src/
│   ├── OrderSystem.Api/
│   └── OrderSystem.Notifications/     <- nuevo proyecto minimal API
├── Dockerfile                         <- build de ordersystem-api
├── Dockerfile.notifications           <- build de notifications-api
├── .dockerignore
├── docker-compose.yml
└── k8s/
    ├── namespace.yaml
    ├── configmap.yaml
    ├── deployment.yaml
    └── service.yaml
```

---

## Checklist

### 1. Preparacion
- [x] Copiar el codigo del reto2 como base del reto3

### 2. Servicio de Notificaciones
- [x] Crear proyecto `OrderSystem.Notifications` (minimal API)
- [x] Implementar endpoint `POST /notify` que reciba y loguee la notificacion
- [x] Verificar que corre localmente

### 3. Modificaciones a la API principal
- [x] Agregar `HttpClient` que llame a `http://notifications-api/notify` al crear una orden
- [x] Agregar health checks en `Program.cs` (`/health` y `/health/ready`)
- [x] Verificar comunicacion local entre ambos servicios

### 4. Docker — ordersystem-api
- [x] Crear `.dockerignore`
- [x] Crear `Dockerfile` multi-stage para la API principal
- [x] Construir imagen: `docker build -t ordersystem-api:latest .`
- [x] Verificar que corre: `docker run -p 8080:8080 ordersystem-api:latest`

### 5. Docker — notifications-api
- [x] Crear `Dockerfile.notifications` multi-stage
- [x] Construir imagen: `docker build -f Dockerfile.notifications -t notifications-api:latest .`

### 6. Docker Compose
- [x] Crear `docker-compose.yml` con ambos servicios en la misma red
- [x] Levantar: `docker-compose up --build`
- [x] Crear una orden y verificar que notifications-api recibe el aviso en sus logs

### 7. Kubernetes
- [x] Crear `k8s/namespace.yaml`
- [x] Crear `k8s/configmap.yaml`
- [x] Crear `k8s/deployment.yaml` con liveness y readiness probes
- [x] Crear `k8s/service.yaml` tipo NodePort (:30080)
- [x] Aplicar manifiestos: `kubectl apply -f k8s/`
- [x] Verificar pods corriendo: `kubectl get pods -n ordersystem`

### 8. Escalado y Self-Healing
- [x] Escalar a 3 replicas: `kubectl scale deployment ordersystem-api --replicas=3 -n ordersystem`
- [x] Verificar 3 pods en Running
- [x] Eliminar un pod: `kubectl delete pod <nombre> -n ordersystem`
- [x] Capturar evidencia de recreacion automatica (`kubectl get pods -n ordersystem -w`)

### 9. Entregables
- [x] Repositorio actualizado con todo el codigo
- [x] Archivos YAML en carpeta `k8s/`
- [x] Evidencias de escalado y self-healing (capturas o logs)
- [x] README con decisiones arquitectonicas y trade-offs
