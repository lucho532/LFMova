# LFMova

Plataforma de gestión y operación de transporte empresarial multiempresa.

Este repositorio sigue un flujo de desarrollo guiado por especificación (SDD). Antes de leer o modificar código, consulta:

* `AGENTS.md` — reglas obligatorias para la implementación.
* `docs/sdd/00-governance/constitution.md` — principios y reglas de negocio aprobadas.
* `docs/sdd/01-specification/spec.md` — especificación funcional.
* `docs/sdd/02-data-model/data-model.md` — modelo de datos.
* `docs/sdd/03-architecture/plan.md` — plan técnico de implementación.
* `docs/sdd/03-architecture/overview.md` — visión general breve de la arquitectura (capas, flujo de datos, autenticación, multiempresa).
* `docs/sdd/04-execution/tasks.md` — tareas ejecutables.
* `docs/sdd/05-validation/checklist.md` — checklist de validación.

## Estado actual

Aplicación funcional y desplegada en Railway (API, web y PostgreSQL). Incluye autenticación con autorregistro y verificación por correo (Brevo), multiempresa y roles, importación de programaciones y de rutas ya armadas por Excel, planificación y asignación de rutas, ejecución de la ruta por el conductor (con ubicación GPS obligatoria), chat de un solo sentido, incidencias con evidencia, facturación por conductor e informes de asistencia. El detalle de cada decisión está en `AGENTS.md` y en `docs/sdd/`.

El frontend (React) se sirve como web/PWA y como app de Android con Capacitor (ver `frontend/README.md`).

## Arquitectura

Monolito modular por capas:

```text
LFMova.Api
        ↓
LFMova.Application
        ↓
LFMova.Domain

LFMova.Infrastructure
        ↓
LFMova.Application
        ↓
LFMova.Domain
```

## Estructura de la solución

El backend (.NET) vive en `backend/`, igual que el frontend vive en `frontend/`:

```text
backend/
├── LFMova.sln
├── src/
│   ├── LFMova.Api/            Controllers, Middleware, Configuration
│   ├── LFMova.Application/    DTOs, Interfaces, Services, Implementations, Mappers, Validators, Utils
│   ├── LFMova.Domain/         Entities, Enums, Rules
│   └── LFMova.Infrastructure/ Data, Repositories, Configurations, Migrations
└── tests/
    ├── Unit/LFMova.UnitTests/
    └── Integration/LFMova.IntegrationTests/
```

## Tecnologías

* C# / .NET 8 (LTS)
* ASP.NET Core Web API
* Entity Framework Core + Npgsql (PostgreSQL)
* Swagger / OpenAPI (Swashbuckle)
* xUnit
* Frontend: React + Vite + TypeScript, empaquetado para Android con Capacitor
* Correo transaccional: Brevo

## Requisitos previos

* .NET SDK 8.0
* PostgreSQL (local o mediante Docker, ver sección "Docker" abajo)

## Compilar y ejecutar

```bash
cd backend
dotnet restore
dotnet build
dotnet run --project src/LFMova.Api
```

## Ejecutar pruebas

```bash
cd backend
dotnet test
```

## Configuración local

La cadena de conexión a PostgreSQL se lee desde `ConnectionStrings:LFMovaDb`.
Para desarrollo local, no coloques credenciales reales en `appsettings.Development.json`;
usa en su lugar los secretos de usuario de .NET:

```bash
dotnet user-secrets set "ConnectionStrings:LFMovaDb" "Host=localhost;Port=5432;Database=lfmova_dev;Username=...;Password=..." --project backend/src/LFMova.Api
```

Alternativa: copiar `backend/src/LFMova.Api/appsettings.Development.json.example` a
`backend/src/LFMova.Api/appsettings.Development.json` (ignorado por git) y completar los valores reales.

## Migraciones

Toda modificación del modelo se refleja en una migración de EF Core (`backend/src/LFMova.Infrastructure/Migrations`). Para aplicarlas manualmente: `dotnet ef database update --project backend/src/LFMova.Infrastructure --startup-project backend/src/LFMova.Api`.

Con `AplicarMigracionesAlIniciar=true` (variable de entorno del servicio) la API aplica las migraciones pendientes al arrancar, antes de atender solicitudes; por defecto no hace nada. Si una migración falla, el arranque se detiene.

## Despliegue

Producción corre en Railway: servicio `api` (`backend/src/LFMova.Api/Dockerfile`), servicio `web` (`frontend/Dockerfile`, Caddy) y PostgreSQL. La dirección de la API se graba en el frontend con `VITE_API_URL` (`frontend/.env.production.local`). La publicación de la app de Android (APK/AAB) se describe en `frontend/README.md`.

## Docker

Entorno reproducible con la API y PostgreSQL (ver `docs/sdd/03-architecture/plan.md` §45). El frontend se ejecuta aparte (ver `frontend/README.md`).

```bash
cp .env.example .env
# completar POSTGRES_PASSWORD y JWT_CLAVE_SECRETA en .env
docker compose up --build
```

La API queda disponible en `http://localhost:8080`. PostgreSQL queda expuesto en el host en el puerto `5433` (no `5432`, para no chocar con un PostgreSQL local ya instalado). La primera vez, aplicar las migraciones contra la base del contenedor:

```bash
dotnet ef database update \
  --project backend/src/LFMova.Infrastructure \
  --startup-project backend/src/LFMova.Api \
  --connection "Host=localhost;Port=5433;Database=lfmova;Username=postgres;Password=<la de tu .env>"
```

`docker-compose.yml` y `.env.example` están versionados; `.env` (con los valores reales) nunca debe versionarse — ya está en `.gitignore`.
