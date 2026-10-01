# TransportApp

Plataforma de gestión y operación de transporte empresarial multiempresa.

Este repositorio sigue un flujo de desarrollo guiado por especificación (SDD). Antes de leer o modificar código, consulta:

* `AGENTS.md` — reglas obligatorias para la implementación.
* `docs/sdd/00-governance/CONSTITUTION.md` — principios y reglas de negocio aprobadas.
* `docs/sdd/01-specification/spec.md` — especificación funcional.
* `docs/sdd/02-data-model/data-model.md` — modelo de datos.
* `docs/sdd/03-architecture/plan.md` — plan técnico de implementación.
* `docs/sdd/03-architecture/overview.md` — visión general breve de la arquitectura (capas, flujo de datos, autenticación, multiempresa).
* `docs/sdd/04-execution/tasks.md` — tareas ejecutables.
* `docs/sdd/05-validation/checklist.md` — checklist de validación.

## Estado actual

Esqueleto técnico inicial de la solución. Todavía **no** incluye entidades de negocio,
endpoints funcionales, autenticación ni ninguna regla de negocio.

## Arquitectura

Monolito modular por capas:

```text
TransportApp.Api
        ↓
TransportApp.Application
        ↓
TransportApp.Domain

TransportApp.Infrastructure
        ↓
TransportApp.Application
        ↓
TransportApp.Domain
```

## Estructura de la solución

El backend (.NET) vive en `backend/`, igual que el frontend vive en `frontend/`:

```text
backend/
├── TransportApp.sln
├── src/
│   ├── TransportApp.Api/            Controllers, Middleware, Configuration
│   ├── TransportApp.Application/    DTOs, Interfaces, Services, Implementations, Mappers, Validators, Utils
│   ├── TransportApp.Domain/         Entities, Enums, Rules
│   └── TransportApp.Infrastructure/ Data, Repositories, Configurations, Migrations
└── tests/
    ├── Unit/TransportApp.UnitTests/
    └── Integration/TransportApp.IntegrationTests/
```

## Tecnologías

* C# / .NET 8 (LTS)
* ASP.NET Core Web API
* Entity Framework Core + Npgsql (PostgreSQL)
* Swagger / OpenAPI (Swashbuckle)
* xUnit

## Requisitos previos

* .NET SDK 8.0
* PostgreSQL (local o mediante Docker, ver sección "Docker" abajo)

## Compilar y ejecutar

```bash
cd backend
dotnet restore
dotnet build
dotnet run --project src/TransportApp.Api
```

## Ejecutar pruebas

```bash
cd backend
dotnet test
```

## Configuración local

La cadena de conexión a PostgreSQL se lee desde `ConnectionStrings:TransportAppDb`.
Para desarrollo local, no coloques credenciales reales en `appsettings.Development.json`;
usa en su lugar los secretos de usuario de .NET:

```bash
dotnet user-secrets set "ConnectionStrings:TransportAppDb" "Host=localhost;Port=5432;Database=transportapp_dev;Username=...;Password=..." --project backend/src/TransportApp.Api
```

Alternativa: copiar `backend/src/TransportApp.Api/appsettings.Development.json.example` a
`backend/src/TransportApp.Api/appsettings.Development.json` (ignorado por git) y completar los valores reales.

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
  --project backend/src/TransportApp.Infrastructure \
  --startup-project backend/src/TransportApp.Api \
  --connection "Host=localhost;Port=5433;Database=transportapp;Username=postgres;Password=<la de tu .env>"
```

`docker-compose.yml` y `.env.example` están versionados; `.env` (con los valores reales) nunca debe versionarse — ya está en `.gitignore`.
