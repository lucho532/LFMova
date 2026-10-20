# LFMova — Frontend

Frontend de la Plataforma de Transporte Empresarial. React + Vite + TypeScript, independiente de `LFMova.sln` (ver `docs/sdd/03-architecture/plan.md` §57).

## Estructura

```text
src/
├── componentes/  componentes de UI reutilizables
├── paginas/      pantallas de la aplicación
├── hooks/        hooks (use*) con la lógica de las pantallas
├── servicios/    clientes de la API REST (uno por recurso del backend) y utilidades de dominio
├── rutas/        enrutamiento y rutas protegidas
├── modelos/      tipos TypeScript que reflejan los DTOs de Application
├── contexto/     autenticación (token JWT), tema y notificaciones
└── estilos/      CSS por componente y por página (componentes/, paginas/)
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
dotnet run --project ../backend/src/LFMova.Api
```

En otra terminal, iniciar el frontend:

```bash
npm install
npm run dev
```

Abrir `http://localhost:5173`. La pantalla inicial es el inicio de sesión (`/iniciar-sesion`); tras iniciar sesión redirige según el rol (coordinador, conductor o empleado).

## Otros comandos

```bash
npm run build     # build de producción (incluye chequeo de TypeScript)
npm run lint      # linting con oxlint
npm run preview   # sirve el build de producción localmente
```

## App de Android (Capacitor)

La misma base de código se empaqueta como app de Android (`frontend/android`). La dirección de la API se toma de `VITE_API_URL` al compilar; para producción vive en `.env.production.local` (fuera de git).

Compilar exige Java 21 y el archivo `android/keystore.properties` con la firma (fuera de git). Antes de compilar hay que subir `versionCode` y `versionName` en `android/app/build.gradle`:

```bash
npm run build
npx cap sync android
cd android
./gradlew assembleRelease bundleRelease
```

Resultados: `app/build/outputs/apk/release/app-release.apk` y `app/build/outputs/bundle/release/app-release.aab` (el que se sube a Google Play). La app instalada lleva el JavaScript empaquetado al compilar, así que cuando cambia la API hay que publicar una versión nueva.
