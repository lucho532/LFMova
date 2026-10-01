# LFMova — Frontend

Frontend de la Plataforma de Transporte Empresarial. React + Vite + TypeScript, independiente de `LFMova.sln` (ver `docs/sdd/03-architecture/plan.md` §57).

## Estructura

```text
src/
├── componentes/  componentes de UI reutilizables
├── paginas/      pantallas de la aplicación
├── servicios/    clientes de la API REST (uno por recurso del backend)
├── rutas/        enrutamiento y rutas protegidas
├── modelos/      tipos TypeScript que reflejan los DTOs de Application
└── contexto/     contexto de autenticación (token JWT)
```

## Requisitos previos

* Node.js 20+
* La API backend (`LFMova.Api`) ejecutándose localmente.

## Configuración

Copiar `.env.example` a `.env.development` (o `.env.local`) y ajustar `VITE_API_URL` a la URL donde corre la API. Por defecto, con el perfil `http` de la API (`dotnet run` desde `src/LFMova.Api`), la URL es `http://localhost:5109`.

La API debe tener configurado el origen del frontend en `Cors:OrigenesPermitidos` (ver `appsettings.Development.json` del backend); por defecto ya incluye `http://localhost:5173`, el puerto por defecto de Vite.

## Ejecutar en desarrollo

En una terminal, iniciar el backend:

```bash
dotnet run --project ../src/LFMova.Api
```

En otra terminal, iniciar el frontend:

```bash
npm install
npm run dev
```

Abrir `http://localhost:5173`. La pantalla inicial es el inicio de sesión (`/iniciar-sesion`); tras iniciar sesión redirige a `/empresas`.

## Otros comandos

```bash
npm run build     # build de producción (incluye chequeo de TypeScript)
npm run lint      # linting con oxlint
npm run preview   # sirve el build de producción localmente
```

## Alcance actual

Solo se implementan las pantallas que corresponden a funcionalidad ya existente en el backend: inicio de sesión y gestión de empresas (crear/listar). No se anticipan pantallas de funcionalidades todavía no implementadas.
