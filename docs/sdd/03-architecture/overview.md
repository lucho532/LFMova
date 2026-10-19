# Visión general de la arquitectura

Documento breve para orientar a quien se incorpore al proyecto. No sustituye a
`plan.md` (referencia técnica completa) ni a `data-model.md`/`CONSTITUTION.md`
(fuente de verdad de reglas de negocio); es un mapa de alto nivel.

## 1. Capas

Monolito modular por capas, con dependencias en una sola dirección:

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

`Domain` no depende de ninguna otra capa. `Application` depende solo de
`Domain`. `Api` e `Infrastructure` dependen de `Application` y `Domain`, pero
nunca una de la otra directamente (se conectan mediante inyección de
dependencias: `Api` resuelve las interfaces de `Application`, cuyas
implementaciones concretas viven en `Infrastructure`).

## 2. Responsabilidades

* **`LFMova.Domain`** — Entidades (`Servicio`, `Jornada`, `Conductor`,
  etc.), enums y reglas de negocio puras (`Reglas*.cs`, p. ej.
  `ReglasUnidadOperativa.HayConflictoTemporal`). Sin dependencias externas, sin
  Entity Framework, sin conocimiento de HTTP.
* **`LFMova.Application`** — Casos de uso (`*Servicio.cs`,
  ej. `ConductorServicio`, `ServicioAutenticacion`), DTOs de entrada/salida,
  `Mappers` (entidad ↔ DTO), `Validators` (validación de formato, no de
  negocio) e interfaces de repositorio (`I*Repositorio`) que `Infrastructure`
  implementa. Coordina el dominio; no decide autorización (eso es de `Api`) ni
  accede directamente a EF Core.
* **`LFMova.Infrastructure`** — `LFMovaDbContext` (EF Core +
  Npgsql), configuraciones de mapeo, migraciones, implementaciones de
  `I*Repositorio`, hasheo de contraseñas y generación de JWT
  (`GeneradorTokenJwt`).
* **`LFMova.Api`** — Controllers delgados (sin lógica de negocio),
  autenticación/autorización (JWT, políticas), Swagger y configuración de
  arranque (`Program.cs`). Cada Controller delega en un `*Servicio` de
  `Application` y solo decide si la petición está autorizada para la empresa
  solicitada.

## 3. Flujo de datos

```text
HTTP request
   ↓
Controller (Api)          — valida autorización de empresa, sin lógica de negocio
   ↓ (DTO de entrada)
*Servicio (Application)   — caso de uso: valida formato, aplica reglas de Domain
   ↓
Reglas de Domain           — invariantes de negocio puras
   ↓
I*Repositorio (Application, implementado en Infrastructure)
   ↓
LFMovaDbContext (Infrastructure) → PostgreSQL
   ↓ (entidad)
Mapper (Application)       — entidad → DTO de salida
   ↓
Controller (Api)           — devuelve el DTO como respuesta HTTP
```

Las entidades de `Domain` nunca se serializan directamente hacia el cliente:
siempre cruzan la frontera de `Api` como DTOs, construidos por los `Mappers`
de `Application`.

## 4. Autenticación

```text
Cédula + contraseña
        ↓
ServicioAutenticacion.IniciarSesionAsync   (Application)
        ↓ (verifica Usuario.Activo, PasswordHash, roles activos)
GeneradorTokenJwt.GenerarToken             (Infrastructure)
        ↓
JWT firmado (HMAC-SHA256)
```

* Las contraseñas se almacenan con hash (`IHasheadorContrasenas`), nunca en
  texto plano.
* Cada rol activo del usuario (`UsuarioRol`) se codifica como un claim `rol`
  independiente en el token: `"ROL:EmpresaId"` para roles con empresa
  (`COORDINADOR`, `EMPLEADO`) o `"ROL"` para roles globales
  (`CONDUCTOR`, `ADMINISTRADOR_PLATAFORMA`) — ver
  `GeneradorTokenJwt` y `data-model.md` §4.1.
* Un usuario sin ningún `UsuarioRol` activo no puede iniciar sesión, aunque su
  contraseña sea correcta.
* Los Controllers verifican autorización con extensiones sobre
  `ClaimsPrincipal` (`ClaimsPrincipalExtensions.cs`): `TieneRolEnEmpresa`,
  `TieneRolGlobal`, `EsUsuario`; y con políticas declarativas
  (`[Authorize(Policy = "RequiereAdministradorPlataforma")]`, evaluadas por
  `ManejadorRequisitoRol`).

## 5. Multiempresa

Ningún endpoint confía en un `EmpresaId` recibido del cliente para decidir
acceso. El patrón obligatorio en cada Controller que expone un recurso
perteneciente a una empresa es:

```csharp
if (!EsAdministradorPlataforma() && !User.TieneRolEnEmpresa(Rol.COORDINADOR, empresaId))
{
    return Forbid();
}
```

es decir: el `EmpresaId` de la URL se contrasta contra los claims del JWT del
usuario autenticado, no al revés. La única excepción es
`ADMINISTRADOR_PLATAFORMA`, un rol global (`EmpresaId = NULL`) que puede
operar sobre cualquier empresa.

La vinculación de un `CONDUCTOR` con una o varias empresas no se modela como
`UsuarioRol` por empresa (el rol `CONDUCTOR` es único y global por usuario),
sino mediante `VinculacionConductorEmpresa` — ver `data-model.md` §4.1 y §10.

## 6. Dónde mirar primero

| Pregunta | Dónde |
|---|---|
| ¿Qué debe hacer el sistema? | `docs/sdd/01-specification/spec.md` |
| ¿Cómo se modelan los datos? | `docs/sdd/02-data-model/data-model.md` |
| ¿Cómo se implementa técnicamente? | `docs/sdd/03-architecture/plan.md` |
| ¿Qué falta por hacer / qué se hizo y cómo se verificó? | `docs/sdd/04-execution/tasks.md` |
| ¿Qué reglas de negocio no pueden violarse? | `docs/sdd/00-governance/CONSTITUTION.md` |
