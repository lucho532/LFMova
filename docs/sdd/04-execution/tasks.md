# Tasks — Plataforma de Gestión de Transporte

## 1. Propósito

Este documento transforma `spec.md`, `data-model.md`, `plan.md`, `CONSTITUTION.md` y `AGENTS.md` en tareas ejecutables.

Claude Code debe ejecutar las tareas en el orden indicado, respetando dependencias y sin introducir decisiones arquitectónicas no definidas en los documentos SDD.

Una tarea funcional no puede marcarse como completada únicamente porque el código compila.

Una tarea se considera completada cuando:

* está implementada;
* el código compila;
* cumple la especificación;
* respeta la arquitectura definida;
* tiene validaciones correspondientes;
* tiene pruebas correspondientes implementadas y ejecutadas, con resultado verificado, antes de marcarse `[X]` — no basta con que existan tareas de prueba planificadas para una fase posterior;
* no rompe funcionalidades existentes (sin regresiones conocidas);
* mantiene el código y documentación en español;
* cumple las reglas de `CONSTITUTION.md` y `AGENTS.md`.

---

# Fase 1 — Creación de la solución

## [X] T001 — Crear solución .NET

Crear la solución principal de la aplicación utilizando .NET y C#.

Debe existir una solución con los siguientes proyectos:

* `LFMova.Api`
* `LFMova.Application`
* `LFMova.Domain`
* `LFMova.Infrastructure`
* `LFMova.UnitTests`
* `LFMova.IntegrationTests`

Dependencias:

* Api → Application
* Api → Infrastructure
* Application → Domain
* Infrastructure → Application
* Infrastructure → Domain
* UnitTests → Application/Domain
* IntegrationTests → Api/Infrastructure

No crear dependencias inversas.

---

## [X] T002 — Crear estructura de carpetas

Crear la estructura definida en `plan.md`.

### Api

* Controllers
* Middleware
* Configuration

### Application

* DTOs
* Interfaces
* Services
* Implementations
* Mappers
* Validators
* Utils

### Domain

* Entities
* Enums
* Rules

### Infrastructure

* Data
* Repositories
* Configurations
* Migrations

---

## [X] T003 — Configurar documentación XML

Configurar la solución para generar documentación XML.

Todo tipo público creado deberá tener documentación XML en español.

Cada clase debe explicar:

* qué representa;
* cuál es su responsabilidad;
* qué no debe hacer cuando sea relevante.

---

## [X] T004 — Configurar análisis y formato

Configurar las reglas de compilación, nullable reference types y formato consistente.

No introducir analizadores o herramientas adicionales innecesarias.

---

# Fase 2 — Dominio

## [X] T005 — Crear enums del dominio

Crear los enums definidos por la especificación:

* `Rol`
* `TipoServicio`
* `EstadoServicio`
* `EstadoServicioPasajero`
* `TipoIncidencia`
* `TipoEvidencia`
* `EstadoImportacionExcel`

Los nombres deben estar en español.

---

## [X] T006 — Crear entidad Usuario

Crear `Usuario` con:

* `UsuarioId`
* `Cedula`
* `PasswordHash`
* `Activo`

Reglas:

* `Cedula` es única globalmente.
* `PasswordHash` puede ser nulo para cuentas pendientes de activación.
* No agregar `Rol`.
* No agregar `EmpresaId`.

---

## [X] T007 — Crear entidad UsuarioRol

Crear `UsuarioRol` con:

* `UsuarioRolId`
* `UsuarioId`
* `Rol`
* `EmpresaId`
* `Activo`

Reglas:

* Un usuario puede tener múltiples roles.
* `EmpresaId` es obligatorio para `COORDINADOR` y `EMPLEADO`.
* `EmpresaId` es `NULL` para `ADMINISTRADOR_PLATAFORMA` (rol global).
* `EmpresaId` es `NULL` para `CONDUCTOR`; la relación con cada empresa se resuelve mediante `VinculacionConductorEmpresa`, no mediante `UsuarioRol.EmpresaId`. No crear un `UsuarioRol CONDUCTOR` por cada empresa vinculada.
* Un usuario nunca puede autoasignarse roles.

---

## [X] T008 — Crear entidad Empresa

Crear `Empresa`:

* `EmpresaId`
* `Nombre`
* `Activa`

No agregar campos no definidos en el modelo.

---

## [X] T009 — Crear entidad Sede

Crear `Sede`:

* `SedeId`
* `EmpresaId`
* `Nombre`
* `Direccion`
* `Ciudad`
* `Barrio`
* `Latitud`
* `Longitud`
* `Activa`

Latitud y longitud deben permitir valores nulos.

---

## [X] T010 — Crear entidad Empleado

Crear `Empleado`:

* `EmpleadoId`
* `UsuarioId`
* `EmpresaId`
* `NombreCompleto`
* `Telefono`
* `Direccion`
* `Barrio`
* `Activo`

La cédula no debe duplicarse en esta entidad.

---

## [X] T011 — Crear entidad Conductor

Crear `Conductor`:

* `ConductorId`
* `UsuarioId`
* `NombreCompleto`
* `Telefono`
* `Activo`

El conductor debe estar asociado a un `Usuario`.

---

## [X] T012 — Crear entidad VinculacionConductorEmpresa

Crear:

* `VinculacionConductorEmpresaId`
* `ConductorId`
* `EmpresaId`
* `Activa`

Regla:

`ConductorId + EmpresaId` debe ser único.

---

## [X] T013 — Crear entidad Vehiculo

Crear:

* `VehiculoId`
* `ConductorId`
* `Placa`
* `Marca`
* `Modelo`
* `Capacidad`
* `Activo`

La placa debe ser única.

Un vehículo pertenece exclusivamente a un conductor.

---

## [X] T014 — Crear entidad UnidadOperativa

Crear:

* `UnidadOperativaId`
* `ConductorId`
* `VehiculoId`
* `Activa`

Reglas:

* un vehículo no puede pertenecer a más de una unidad;
* conductor y vehículo deben pertenecer al mismo conductor.

---

## [X] T015 — Crear entidad ImportacionExcel

Crear:

* `ImportacionExcelId`
* `EmpresaId`
* `CoordinadorId`
* `NombreArchivo`
* `FechaImportacion`
* `Estado`

`Estado` utiliza los valores: `PENDIENTE`, `PROCESANDO`, `COMPLETADA`, `COMPLETADA_CON_ADVERTENCIAS`, `ERROR`.

`CoordinadorId` referencia al `Usuario` que realizó la importación.

---

## [X] T016 — Crear entidad ProgramacionTransporte

Crear:

* `ProgramacionTransporteId`
* `EmpresaId`
* `EmpleadoId`
* `SedeId`
* `Fecha`
* `Hora`
* `Tipo`
* `DireccionRecogida`
* `BarrioRecogida`

Mantener `Fecha` y `Hora` como propiedades independientes.

---

## [X] T017 — Crear entidad Jornada

Crear:

* `JornadaId`
* `EmpresaId`
* `UnidadOperativaId`
* `FechaOperativa`

No agregar:

* HoraInicio
* HoraFin

No crear restricción única entre `UnidadOperativaId` y `FechaOperativa`.

---

## [X] T018 — Crear entidad Servicio

Crear:

* `ServicioId`
* `JornadaId`
* `SedeId`
* `Fecha`
* `HoraProgramada`
* `Tipo`
* `Estado`
* `HoraInicioReal`
* `HoraFinReal`

No agregar:

* `EmpresaId`
* `UnidadOperativaId`

La empresa se obtiene a través de `Jornada`.

---

## [X] T019 — Crear entidad ServicioPasajero

Crear:

* `ServicioPasajeroId`
* `ServicioId`
* `ProgramacionTransporteId`
* `EmpleadoId`
* `Estado`
* `Orden`
* `DireccionRecogida`
* `Latitud`
* `Longitud`

Crear restricción única para `ProgramacionTransporteId`.

---

## [X] T020 — Crear entidad UbicacionRecogidaHistorica

Crear:

* `UbicacionRecogidaHistoricaId`
* `EmpleadoId`
* `Direccion`
* `Barrio`
* `Latitud`
* `Longitud`
* `FechaRegistro`

`FechaRegistro` permite determinar cuál es la ubicación histórica más reciente.

---

## [X] T021 — Crear entidad Incidencia

Crear:

* `IncidenciaId`
* `ServicioPasajeroId`
* `Tipo`
* `Descripcion`
* `FechaHora`
* `Latitud`
* `Longitud`

---

## [X] T022 — Crear entidad Evidencia

Crear:

* `EvidenciaId`
* `IncidenciaId`
* `Tipo`
* `ReferenciaArchivo`
* `FechaHora`

`Tipo` utiliza inicialmente un único valor: `FOTOGRAFIA`.

La evidencia debe almacenar una referencia al archivo y no el archivo binario dentro de PostgreSQL.

---

## [X] T023 — Crear entidad Conversacion

Crear:

* `ConversacionId`
* `ServicioPasajeroId`

Un `ServicioPasajero` puede tener como máximo una conversación.

---

## [X] T024 — Crear entidad Mensaje

Crear:

* `MensajeId`
* `ConversacionId`
* `UsuarioId`
* `Contenido`
* `FechaHora`

---

## [X] T025 — Crear entidad Notificacion

Crear:

* `NotificacionId`
* `UsuarioId`
* `Tipo`
* `Titulo`
* `Mensaje`
* `Leida`
* `FechaHora`

---

# Fase 3 — Reglas de dominio

## [X] T026 — Validar consistencia multiempresa

Crear reglas de dominio para garantizar que:

* Empleado pertenece a la Empresa correspondiente.
* Sede pertenece a la Empresa.
* ProgramacionTransporte pertenece a la Empresa.
* Jornada pertenece a la Empresa.
* Servicio utiliza una Jornada de la empresa correspondiente.
* Servicio utiliza una Sede de la misma empresa.
* Un coordinador solamente opera sobre empresas a las que está vinculado.

---

## [X] T027 — Validar UnidadOperativa

Implementar regla:

`UnidadOperativa.ConductorId == Vehiculo.ConductorId`

No permitir guardar una unidad inconsistente.

---

## [X] T028 — Validar relaciones de ServicioPasajero

Validar que:

* ServicioPasajero pertenece al Servicio indicado.
* ProgramacionTransporte pertenece al mismo contexto empresarial.
* Empleado corresponde a la programación.
* una programación no pueda estar asignada a más de un ServicioPasajero.

---

## [X] T029 — Implementar reglas de roles

Implementar autorización de:

* `ADMINISTRADOR_PLATAFORMA`
* `COORDINADOR`
* `CONDUCTOR`
* `EMPLEADO`

Validar que `UsuarioRol.EmpresaId` sea obligatorio para `COORDINADOR` y `EMPLEADO`, y `NULL` para `ADMINISTRADOR_PLATAFORMA` y `CONDUCTOR`.

Validar que un mismo usuario no tenga más de un `UsuarioRol COORDINADOR` activo en empresas diferentes.

La autorización debe ejecutarse en backend.

La interfaz React nunca debe ser considerada mecanismo de seguridad.

---

# Fase 4 — Persistencia

## [X] T030 — Configurar DbContext

Crear el contexto EF Core principal.

Configurar todas las entidades y relaciones.

---

## [X] T031 — Configurar relaciones Empresa

Configurar:

* Empresa → Sede
* Empresa → Empleado
* Empresa → ImportacionExcel
* Empresa → ProgramacionTransporte
* Empresa → Jornada
* Empresa ↔ Conductor mediante `VinculacionConductorEmpresa`

---

## [X] T032 — Configurar relaciones Usuario

Configurar:

* Usuario → UsuarioRol
* Usuario → Empleado
* Usuario → Conductor
* Usuario → Mensaje
* Usuario → Notificacion

Garantizar las relaciones 1:0..1 de:

* Usuario → Empleado
* Usuario → Conductor

---

## [X] T033 — Configurar relaciones operativas

Rehecha conforme al modelo corregido (ver `data-model.md` §15-16 y `plan.md` §26-27): `UnidadOperativa → Servicio` (`Servicio.UnidadOperativaId`, opcional); `Jornada` ya no tiene `UnidadOperativaId`. Migración `CorregirRelacionServicioUnidadOperativaYAgregarCodigoActivacion` aplicada.

Configurar:

* Conductor → Vehiculo
* Conductor → UnidadOperativa
* Vehiculo → UnidadOperativa
* UnidadOperativa → Servicio
* Jornada → Servicio
* Servicio → ServicioPasajero
* ProgramacionTransporte → ServicioPasajero (relación `1:0..1`, con restricción `UNIQUE(ProgramacionTransporteId)` sobre `ServicioPasajero`)

---

## [X] T034 — Configurar relaciones históricas y comunicación

Configurar:

* Empleado → UbicacionRecogidaHistorica
* ServicioPasajero → Incidencia
* Incidencia → Evidencia
* ServicioPasajero → Conversacion
* Conversacion → Mensaje

---

## [X] T035 — Configurar índices y restricciones

Crear los índices necesarios para:

* `Usuario.Cedula`
* `Vehiculo.Placa`
* `ConductorId + EmpresaId`
* `VehiculoId`
* `ProgramacionTransporteId`
* `ServicioPasajero`
* campos utilizados frecuentemente para búsquedas por empresa y fecha.

No crear índices innecesarios sin justificación.

---

## [X] T036 — Crear migración inicial

Crear la migración inicial de EF Core.

Verificar que la migración representa exactamente el modelo definido.

---

## [X] T037 — Crear base PostgreSQL de desarrollo

Configurar conexión a PostgreSQL mediante configuración externa.

Nunca almacenar credenciales directamente en código.

---

# Fase 5 — Autenticación

## [X] T038 — Crear DTOs de autenticación

Crear DTOs para:

* inicio de sesión;
* activación de cuenta (`Cedula`, `Codigo`, `NuevaContrasena`, `ConfirmacionContrasena` — ver T042);
* generación de código de activación (T042A);
* respuesta de autenticación.

---

## [X] T039 — Implementar autenticación por cédula

Implementar login mediante:

`Cedula + Password`

Validar:

* usuario existente;
* usuario activo;
* contraseña correcta;
* roles activos.

---

## [X] T040 — Implementar generación JWT

Crear servicio para generar JWT.

El token debe representar los roles y contexto autorizado del usuario.

No incluir información sensible innecesaria.

---

## [X] T041 — Implementar autorización por roles

Configurar autorización ASP.NET Core para los cuatro roles.

Las operaciones deben verificar además el contexto de empresa cuando corresponda.

---

## [X] T042 — Implementar activación inicial

**Decisión cerrada** (ver `data-model.md` §28 y `spec.md` §10): la activación se valida mediante un código temporal generado exclusivamente por un conductor (T042A). No usar WhatsApp, SMS, email ni proveedor externo. El coordinador y el `ADMINISTRADOR_PLATAFORMA` no participan.

Implementar:

`Activar mi cuenta`

Datos de entrada: `Cedula`, `Codigo`, `NuevaContrasena`, `ConfirmacionContrasena`.

Flujo:

1. el `Usuario` existe y corresponde a la cédula;
2. la cuenta está pendiente de activación (`PasswordHash = NULL`);
3. el código corresponde a ese `Usuario` (comparando su hash, no en texto plano);
4. el código no está expirado;
5. el código no fue utilizado;
6. no se superó el límite de intentos fallidos;
7. se genera y almacena `PasswordHash`;
8. se marca el código como utilizado (`Utilizado = true`);
9. cuenta queda habilitada para login.

Depende de: T042A (debe existir un `CodigoActivacion` vigente antes de poder activarse).

---

## [X] T042A — Generar código de activación (conductor) — OBSOLETA (2026-09-19)

> Reemplazada por el autorregistro con confirmación por correo (`RegistroServicio`, `RecuperacionContrasenaServicio`, `TokenVerificacion`). El código fuente asociado fue eliminado; el texto siguiente se conserva como histórico.

Implementar la acción del conductor:

`Generar código de activación`

Autorización: el conductor solo puede generar un código para un `Usuario` (empleado) que esté asignado, mediante `ServicioPasajero.EmpleadoId`, a algún `Servicio` cuya `UnidadOperativa` pertenezca a ese conductor. No debe existir una vía para que el conductor busque o genere códigos para empleados no vinculados a ninguno de sus servicios.

El conductor no activa la cuenta ni fija la contraseña; únicamente genera el código y lo entrega (presencial o telefónicamente).

Crear la entidad `CodigoActivacion` (ver `data-model.md` §28.2): código único, temporal, de un solo uso, con límite de intentos, y almacenado mediante hash (reutilizar el mismo mecanismo de `IHasheadorContrasenas` ya existente, no crear uno nuevo).

---

## [X] T043 — Implementar soporte para múltiples roles

Un mismo Usuario debe poder tener simultáneamente:

* EMPLEADO
* CONDUCTOR
* COORDINADOR

o cualquier combinación válida.

No crear usuarios duplicados para representar roles.

---

# Fase 6 — Empresas y administración

## [X] T044 — Crear API de empresas

Implementar operaciones para:

* crear empresa;
* consultar empresa;
* activar/desactivar empresa.

Solamente `ADMINISTRADOR_PLATAFORMA` puede crear empresas.

---

## [X] T045 — Crear primer coordinador

Implementar flujo mediante el cual el administrador de plataforma:

1. crea empresa;
2. crea/asocia usuario;
3. asigna `COORDINADOR`;
4. vincula el rol a la empresa.

---

## [X] T046 — Crear coordinadores adicionales

Permitir que un coordinador:

* cree otro coordinador;
* le asigne rol `COORDINADOR`;
* lo vincule exclusivamente a su empresa.

Antes de asignar, validar que el usuario destino no tenga ya un `UsuarioRol COORDINADOR` activo en otra empresa.

No permitir crear empresas.

---

## [X] T046A — Revocar rol COORDINADOR

Permitir que un coordinador autorizado desactive (`Activo = false`) un `UsuarioRol` de tipo `COORDINADOR` perteneciente a su propia empresa.

Aplicar las mismas validaciones de autorización que la asignación (T046).

Antes de ejecutar la revocación, validar que la empresa conserve al menos un `UsuarioRol COORDINADOR` activo distinto del que se va a revocar. Si la revocación dejaría a la empresa sin ningún coordinador activo, la operación debe rechazarse.

No eliminar físicamente el registro `UsuarioRol`.

---

# Fase 7 — Sedes

## [X] T047 — Crear API de sedes

Implementar:

* crear sede;
* consultar sedes;
* modificar sede;
* activar/desactivar sede.

Validar empresa.

---

## [X] T048 — Gestionar coordenadas

Permitir almacenar coordenadas opcionales.

No bloquear una sede si el mecanismo de geocodificación aún no está integrado.

---

# Fase 7B — Frontend inicial (React)

Ver `plan.md` §57 para la arquitectura completa. El frontend vive en `frontend/`, fuera de `LFMova.sln`, y avanza en paralelo con el backend: cada tarea de esta fase consume únicamente endpoints ya implementados y probados.

## [X] T048A — Crear proyecto frontend (Vite + React + TypeScript)

Crear `frontend/` con Vite, React y TypeScript.

Crear la estructura de carpetas: `componentes`, `paginas`, `servicios`, `rutas`, `modelos`, `contexto`.

No agregar al `.sln` de .NET.

No agregar librerías de UI, gestión de estado ni routing adicionales sin necesidad concreta.

## [X] T048B — Configurar cliente HTTP y variables de entorno

Crear un cliente HTTP centralizado en `servicios/` que lea la URL base de la API desde variables de entorno de Vite.

No hardcodear la URL del backend en el código fuente.

## [X] T048C — Implementar inicio de sesión

Crear la página de inicio de sesión (`paginas/`) que consume `POST /api/autenticacion/iniciar-sesion`.

Guardar el token JWT en el contexto de autenticación (`contexto/`) y adjuntarlo como `Authorization: Bearer` en las siguientes solicitudes.

Mostrar el error correspondiente cuando las credenciales sean inválidas.

## [X] T048D — Implementar enrutamiento protegido

Configurar rutas (`rutas/`) que redirigen a la página de inicio de sesión cuando no exista un token válido en el contexto.

Dejar explícito (comentario/documentación del código) que esta protección es únicamente de experiencia de usuario; la autorización real se valida en el backend.

## [X] T048E — Implementar pantalla de gestión de empresas

Crear una página para `ADMINISTRADOR_PLATAFORMA` que liste y permita crear empresas, consumiendo los endpoints ya implementados en `EmpresasController`.

No agregar campos ni acciones que no correspondan a los DTOs ya definidos en el backend (`CrearEmpresaDto`, `EmpresaDto`).

## [X] T048F — Verificar ejecución local

Verificar que `npm run dev` (o equivalente de Vite) sirve la aplicación localmente y permite iniciar sesión y ver/crear empresas contra la API backend en ejecución.

Documentar en `frontend/README.md` cómo ejecutar el frontend localmente junto con el backend.

---

# Fase 8 — Conductores y unidades

## [X] T049 — Crear conductor

Crear flujo para registrar conductor y su Usuario.

---

## [X] T050 — Vincular conductor con empresa

Permitir que un conductor pueda estar vinculado a varias empresas.

No duplicar el conductor.

---

## [X] T051 — Crear vehículo

Permitir registrar:

* placa;
* marca;
* modelo;
* capacidad.

El vehículo debe pertenecer al conductor autenticado/seleccionado según permisos.

---

## [X] T052 — Crear UnidadOperativa

Crear la unidad a partir de:

* conductor;
* vehículo.

Validar que ambos pertenezcan al mismo conductor.

---

## [X] T053 — Gestión de estado

Implementar activación/desactivación de:

* conductor;
* vehículo;
* unidad;
* vinculación con empresa.

Al desactivar un vehículo con `UnidadOperativa` asociada, desactivar también dicha unidad.

Si el conductor comienza a operar con un vehículo nuevo, crear una nueva `UnidadOperativa`; no reutilizar ni modificar la anterior.

Los `Servicio` históricos no deben modificarse para reflejar el cambio de unidad (la unidad operativa se referencia desde `Servicio`, no desde `Jornada`; ver `data-model.md` §15-16).

No eliminar físicamente registros históricos.

---

# Fase 8B — Gestión directa de Empleados

## [X] T053A — Consultar empleados de la empresa

Implementar consulta de empleados filtrada por la empresa del coordinador autenticado.

No permitir consultar empleados de otra empresa modificando el `EmpresaId` en la petición.

---

## [X] T053B — Actualizar datos actuales del empleado

Permitir actualizar `NombreCompleto`, `Telefono`, `Direccion`, `Barrio`.

No modificar registros históricos (`ProgramacionTransporte`, `ServicioPasajero`) como consecuencia de esta actualización.

---

## [X] T053C — Activar/desactivar empleado

Implementar desactivación lógica de `Empleado`. No eliminar físicamente el registro.

---

## [X] T053E — Probar gestión directa de empleados

Cubrir: consulta filtrada por empresa, actualización de datos actuales, activación/desactivación.

Confirmar que no existe ningún endpoint ni operación manual para cambiar la empresa de un empleado, ni para `COORDINADOR` ni para `ADMINISTRADOR_PLATAFORMA`. El cambio de empresa se cubre en T056A/T058A (Fase 9).

Esta tarea debe ejecutarse como parte de la Fase 8B, no diferirse a la Fase 20.

---

# Fase 9 — Importación Excel

## [!] T054 — Definir contrato de Excel

Documentar el formato real de Excel cuando esté disponible.

No inventar columnas definitivas antes de conocer el archivo real.

---

## T055 — Crear servicio de importación

Implementar procesamiento de Excel.

Registrar cada importación en `ImportacionExcel`.

---

## T056 — Crear o actualizar usuarios de empleados

Durante la importación:

* buscar por cédula;
* crear Usuario si no existe;
* crear/actualizar perfil Empleado;
* asignar rol EMPLEADO.

Reutilizar `IEmpleadoService` (Fase 8B) para crear/actualizar el perfil; no duplicar la lógica de creación/actualización.

No crear usuarios duplicados.

---

## T056A — Detectar y sincronizar cambio de empresa del empleado

Durante la importación, si la `Cedula` (identificada mediante `Usuario`) ya existe asociada a un `Empleado` de una empresa distinta a la de la importación actual:

* actualizar `Empleado.EmpresaId` a la nueva empresa;
* actualizar el `EmpresaId` del `UsuarioRol` del rol `EMPLEADO` correspondiente a la nueva empresa;
* ambas actualizaciones deben ejecutarse dentro de la misma transacción.

No existe una operación manual equivalente; esta es la única vía por la que un empleado cambia de empresa.

`ADMINISTRADOR_PLATAFORMA` no participa en este proceso.

No modificar registros existentes de `ProgramacionTransporte`, `ServicioPasajero`, `Jornada` ni `Servicio`; estos conservan la empresa en la que fueron generados.

---

## T057 — Detectar cambios

Comparar la programación importada con la existente.

Casos:

* registro nuevo → crear;
* datos idénticos → mantener;
* datos diferentes → informar al coordinador;
* no eliminar automáticamente programaciones anteriores.

---

## T058 — Registrar ubicación histórica

Cuando una dirección de recogida sea nueva o diferente, permitir conservarla como ubicación histórica.

---

## T058A — Probar cambio de empresa detectado por importación

Cubrir: detección de la misma cédula en una empresa distinta, actualización sincronizada de `Empleado.EmpresaId` y `UsuarioRol(EMPLEADO).EmpresaId` en una misma transacción, y conservación intacta de `ProgramacionTransporte`, `ServicioPasajero`, `Jornada` y `Servicio` históricos.

Esta tarea debe ejecutarse como parte de la Fase 9, no diferirse a la Fase 20.

---

# Fase 10 — Programación de transporte

## [X] T059 — Crear API de programación

Implementar CRUD controlado de `ProgramacionTransporte`.

---

## [X] T060 — Validar programación

Validar:

* empresa;
* empleado;
* sede;
* fecha;
* hora;
* tipo;
* dirección.

---

## [X] T061 — Separar entrada y salida

Garantizar reglas:

### Entrada

Empleado → Sede.

### Salida

Sede → Dirección del empleado.

---

## [~] T062 — Evitar mezcla de sedes

Un Servicio solamente puede contener pasajeros asociados a la misma sede.

---

# Fase 11 — Jornadas y servicios

## [X] T063 — Crear Jornada

Implementar creación de Jornada asociada a:

* empresa;
* fecha operativa.

---

## [X] T064 — Crear Servicio

Implementar creación de servicios asociados a una Jornada, opcionalmente con una unidad operativa ya asignada (`Servicio.UnidadOperativaId`, opcional, validada contra vinculación conductor-empresa).

---

## [X] T065 — Implementar estados de Servicio

Implementar transiciones válidas:

`BORRADOR`

→ `PENDIENTE_ASIGNACION`

→ `ASIGNADO`

→ `PUBLICADO`

→ `EN_CURSO`

→ `FINALIZADO`

También:

`CANCELADO`

según reglas autorizadas.

---

## [X] T066 — Asignar servicios a unidades

**Decisión de modelo cerrada** (ver `data-model.md` §16 y `plan.md` §26-27): `UnidadOperativaId` pertenece a `Servicio`, no a `Jornada`. Reasignar la unidad operativa de un servicio modifica únicamente `Servicio.UnidadOperativaId`; `Servicio.JornadaId` no cambia.

Implementar la reasignación individual de un `Servicio` entre `UnidadOperativa` sin cambiar su `Jornada`.

Debe contemplar:

* la `UnidadOperativa` destino debe estar activa;
* debe pertenecer, mediante su conductor, a una vinculación activa con la misma empresa de la jornada;
* autorización del coordinador de esa empresa;
* conflicto temporal incompatible (ver T067);
* posibilidad de dejar temporalmente el `Servicio` sin unidad (`UnidadOperativaId = NULL`) durante la reorganización, si el estado lo permite;
* prohibición de publicar un `Servicio` sin `UnidadOperativa` asignada (ver T068).

Permitir al coordinador:

* asignar unidad a un servicio;
* quitar la asignación (dejarlo sin unidad);
* redistribuir servicios entre unidades.

No bloquear redistribución por distancia, carga estimada o calidad de ruta; el algoritmo puede proponer o advertir, pero no impedir una reasignación manual válida del coordinador.

---

## [X] T067 — Validar conflicto de unidad

No permitir que una unidad tenga servicios incompatibles temporalmente (solapamiento que impida atenderlos simultáneamente).

Para reasignar:

1. retirar la unidad del servicio anterior (queda con `UnidadOperativaId = NULL`, en un estado que permita reorganización);
2. dejar el servicio anterior pendiente de asignación;
3. asignar la nueva unidad al servicio.

---

## [X] T068 — Validar publicación

No permitir publicar una Jornada si existen servicios:

* sin unidad;
* sin resolución de asignación;
* vacíos cuando corresponda eliminar/reasignar.

---

# Fase 12 — Pasajeros

## [X] T069 — Crear ServicioPasajero

Asignar programación a servicio.

Guardar:

* empleado;
* dirección histórica;
* coordenadas;
* orden.

---

## [X] T070 — Implementar estados de pasajero

Implementar los estados definidos en `spec.md`.

No crear historial de estados.

---

## [X] T071 — Confirmación del empleado

Permitir:

* confirmar;
* indicar que no asistirá.

Cuando indique `NO_ASISTIRA`:

* notificar conductor;
* deshabilitar pasajero operacionalmente;
* no impedir finalizar el servicio.

---

## [X] T072 — Reordenar pasajeros

Permitir al conductor modificar el orden.

Persistir el nuevo valor de `Orden`.

---

## [X] T072A — Gestionar ubicación habitual del empleado

Cuando un empleado proporcione una nueva dirección de recogida (por ejemplo, al confirmar un `ServicioPasajero`), permitir que decida si desea establecerla como su dirección habitual.

Si el empleado sustituye su dirección habitual:

* la dirección anterior puede conservarse como ubicación histórica en `UbicacionRecogidaHistorica` (con su `FechaRegistro`);
* la nueva dirección pasa a ser `Empleado.Direccion` (dirección actual/habitual).

Si el empleado no decide conservarla como habitual, la dirección proporcionada se utiliza únicamente para el `ServicioPasajero` actual y no modifica `Empleado.Direccion`.

No modificar retrospectivamente ningún `ServicioPasajero` existente.

Diferenciar explícitamente:

* `Empleado.Direccion` → dirección actual/habitual;
* `UbicacionRecogidaHistorica` → ubicación anterior utilizada, conservada como histórico;
* `ServicioPasajero.DireccionRecogida` → ubicación exacta e inmutable utilizada para ese servicio específico.

---

# Fase 13 — Publicación y notificaciones

## [X] T073 — Publicar Jornada

**Depende de:** T075 (servicio de `Notificacion`) debe estar implementado antes de ejecutar esta tarea.

Implementar operación `Enviar`.

La publicación debe:

* cambiar estado;
* notificar usuarios afectados;
* dejar la Jornada visible al conductor;
* dejar los servicios correspondientes visibles a empleados.

---

## [X] T074 — Notificar cambios específicos

**Depende de:** T075 (servicio de `Notificacion`).

Si cambia un pasajero de conductor:

* notificar nuevo conductor;
* notificar empleado afectado;
* no notificar usuarios no afectados.

---

## [X] T075 — Implementar Notificacion

Esta tarea debe implementarse antes que T073 y T074, ya que ambas requieren el servicio de notificaciones.

Crear servicio para:

* crear notificación;
* consultar notificaciones;
* marcar como leída.

---

# Fase 14 — Planificación asistida

## [X] T076 — Crear modelo de planificación

Crear componentes necesarios para recibir:

* pasajeros;
* horarios;
* sedes;
* capacidad;
* ubicación;
* servicios previos;
* servicios posteriores.

---

## [X] T077 — Implementar validación de capacidad

Una unidad no puede ser propuesta para un servicio que exceda su capacidad.

---

## [X] T078 — Implementar continuidad geográfica

El algoritmo debe considerar:

* ubicación final del servicio anterior;
* primera recogida del siguiente;
* distancia;
* tiempo estimado de viaje.

---

## [X] T079 — Implementar heurística de entrada

Para entradas considerar como heurística:

* pasajeros más alejados primero;
* progresión hacia la sede;
* tiempo estimado de recorrido;
* llegada a sede al menos 15 minutos antes.

La recomendación no es una obligación.

---

## [X] T080 — Implementar ventana aproximada de recogida

Considerar un tiempo conjunto aproximado de recogida, con un valor inicial de referencia de 25 minutos.

Este valor debe implementarse como parámetro configurable, no como constante fija de negocio, y no debe interpretarse como 25 minutos por pasajero.

El valor definitivo permanece pendiente de validación (ver `spec.md` §47 y `plan.md` §55).

---

## [X] T081 — Generar propuesta de planificación

El algoritmo debe generar una propuesta.

Nunca debe modificar automáticamente la planificación definitiva.

---

## [X] T082 — Permitir aceptación/rechazo del coordinador

El coordinador debe poder:

* aceptar propuesta;
* modificar propuesta;
* rechazar propuesta;
* reorganizar manualmente.

---

# Fase 15 — Ejecución de ruta

## [X] T083 — Iniciar ruta

Permitir al conductor iniciar manualmente una ruta.

Guardar:

`HoraInicioReal`

---

## [!] T084 — Detectar llegada a pasajero

Implementar detección de llegada mediante geolocalización.

Debe existir alternativa manual cuando la detección automática no sea posible.

**Estado parcial:** implementada la alternativa manual (`IServicioPasajeroServicio.MarcarLlegadaAsync`, endpoint `POST .../pasajeros/{id}/marcar-llegada`, exclusivo del conductor asignado, válido solo con el servicio `EN_CURSO`). La detección automática mediante geolocalización queda bloqueada: `spec.md` §26 y `plan.md` §34 dejan explícitamente pendientes el radio de geocerca y el proveedor tecnológico, y este último indica textualmente "No se debe introducir una distancia arbitraria sin decisión previa". No se implementa hasta que exista esa decisión.

---

## [!] T085 — Implementar espera

Implementar contador de espera.

Límite:

`2 minutos`.

El límite debe quedar parametrizable si posteriormente se decide configurarlo.

**Estado parcial:** implementada la transición `CONDUCTOR_LLEGO → ESPERANDO` (`IServicioPasajeroServicio.MarcarEsperandoAsync`, endpoint `POST .../pasajeros/{id}/marcar-esperando`, exclusivo del conductor asignado). No se implementó un contador ni un límite parametrizable en el backend porque `data-model.md` §17.1 no define un campo de instante de inicio de espera en `ServicioPasajero` (la lista de campos es cerrada); sin ese dato no hay nada que el backend pueda contar o comparar contra un límite. El límite de 2 minutos, mientras tanto, es un valor gestionado por la interfaz del conductor (temporizador de cliente), no por el backend.

Adicionalmente (decisión del usuario, 2026-09-18): se implementó un conjunto mínimo de transiciones de "resultado" tras la espera —`ESPERANDO → {RECOGIDO, NO_CONTESTA, NO_SE_ENCUENTRA, DIRECCION_INCORRECTA, NO_SE_PUDO_RECOGER}` y `RECOGIDO → EN_VEHICULO → DEJADO_EN_DESTINO`— mediante `ReglasEstadoServicioPasajero`, `IServicioPasajeroServicio.CambiarEstadoAsync` y el endpoint `PUT .../pasajeros/{id}/estado`, sin generar incidencias automáticas (eso corresponde a la gestión manual de incidencias de la Fase 16). Esto desbloquea la validación de "pasajeros procesados" que exige T090.

---

## [X] T086 — Implementar llamadas

Permitir que el conductor inicie una llamada al empleado mediante las capacidades del dispositivo.

No almacenar grabaciones.

**Implementación:** iniciar la llamada en sí es una capacidad nativa del dispositivo (frontend/móvil, p. ej. un enlace `tel:`), sin lógica de backend. Lo que sí correspondía al backend era exponer el teléfono del empleado al conductor: se agregaron `NombreCompletoEmpleado`/`TelefonoEmpleado` a `ServicioPasajeroDto`, y `GET .../pasajeros` ahora es accesible también para el conductor de la unidad asignada al servicio (antes era exclusivo del coordinador). No se implementa ni se planea almacenamiento de grabaciones.

---

## [X] T087 — Implementar chat

Crear conversación individual:

`Conductor ↔ Empleado`

por cada `ServicioPasajero`.

No implementar chat grupal.

---

## [X] T088 — Compartir ubicación

Permitir que el empleado comparta una ubicación actualizada cuando sea necesario.

---

## [X] T089 — Abrir navegación

Permitir al conductor abrir la ubicación del empleado en la aplicación de mapas disponible.

**Implementación:** abrir la app de mapas es una capacidad nativa del dispositivo (frontend/móvil, p. ej. un enlace `geo:`/`https://maps...` con las coordenadas), sin lógica de backend propia. El backend ya expone lo necesario: `Latitud`/`Longitud` de cada pasajero en `ServicioPasajeroDto`, visible para el conductor desde T086 (`GET .../pasajeros`). No se introdujo un proveedor de mapas definitivo (sigue pendiente, ver T078/T084): el enlace de navegación puede construirse en el cliente con cualquier app de mapas disponible en el dispositivo, sin que el backend dependa de uno específico.

---

## [X] T090 — Registrar finalización

Para entrada:

* validar pasajeros procesados;
* registrar hora final;
* registrar ubicación final.

No permitir finalización manual si existen pasajeros pendientes que deban ser procesados.

Para salida:

* permitir finalizar donde termine el recorrido.

---

# Fase 16 — Incidencias y evidencias

## [X] T091 — Registrar incidencia

Permitir registrar:

* `NO_CONTESTA`
* `NO_SE_ENCUENTRA`
* `DIRECCION_INCORRECTA`
* `NO_SE_PUDO_RECOGER`
* `UBICACION_MODIFICADA`
* `OTRA`

---

## [X] T092 — Registrar ubicación de incidencia

Guardar coordenadas cuando estén disponibles.

**Implementación:** cubierta por el mismo `IIncidenciaServicio.CrearAsync` de T091 (`Latitud`/`Longitud` opcionales en `CrearIncidenciaDto`), ya que ambos campos pertenecen a la misma entidad `Incidencia` y se registran en la misma operación.

---

## [X] T093 — Registrar evidencia

Permitir asociar una fotografía (`Tipo = FOTOGRAFIA`) mediante `ReferenciaArchivo`.

No guardar binarios directamente en PostgreSQL.

---

# Fase 17 — Alertas

## [X] T094 — Implementar alerta de ruta no iniciada

Una hora antes de cada servicio de entrada:

* verificar si la ruta fue iniciada;
* si no lo fue, notificar al conductor responsable.

No notificar esta alerta al coordinador como requisito operacional.

**Implementación:** requería fijar una estrategia de zona horaria (pendiente en el SDD hasta ahora). Decisión del usuario (2026-09-18): toda la plataforma opera en UTC — documentada como cerrada en `AGENTS.md` §41, `spec.md` §47, `plan.md` §48 y `data-model.md` §34. Con esa base: `ReglasAlertaEjecucion.DebeAlertarRutaNoIniciada` (Domain) compara `Fecha`+`HoraProgramada` contra `DateTime.UtcNow`; `IAlertaEjecucionServicio` recorre los servicios `ENTRADA` en `PUBLICADO` y notifica solo al conductor (nunca al coordinador); `AlertaEjecucionBackgroundService` lo ejecuta cada 15 minutos (intervalo de implementación, no regla de negocio). **Limitación conocida:** no hay deduplicación persistida — si el conductor no inicia la ruta, puede recibir la alerta más de una vez dentro de la ventana de una hora, ya que `data-model.md` no define un campo para registrar "alerta ya enviada" y no correspondía inventarlo.

**Actualización (2026-09-30):** la comparación ahora usa la hora de Colombia (`ReglasHoraColombia`), porque `HoraProgramada` se guarda en hora de Colombia: comparar contra UTC hacía que la alerta llegara 5 horas antes. La alerta se envía también a los conductores de la misma empresa con ruta a la misma fecha y hora, con textos legibles (nombre, tipo, hora, sede y fecha). Por decisión del usuario, el aviso se repite cada 15 minutos mientras la ruta siga sin iniciarse (desde 1 hora antes hasta la hora programada: después de la hora de entrada ya no se avisa), con el tiempo que falta; sin campos nuevos, solo se evita reenviar el mismo aviso a la misma persona en menos de 10 minutos. Ver `plan.md` §38 y §48.

---

## [X] T095 — Revisar compatibilidad con hora recomendada

La lógica de alerta debe quedar separada de la futura lógica de hora recomendada de inicio.

No asumir todavía una fórmula definitiva.

**Verificación:** `AlertaEjecucionServicio`/`ReglasAlertaEjecucion` no referencian `PlanificacionServicio`, `ReglasPlanificacion` ni `OpcionesPlanificacion`; la alerta usa exclusivamente `Servicio.HoraProgramada` (el horario fijo ya existente), no una "hora recomendada" calculada por el algoritmo de planificación. Ambas lógicas son independientes por construcción, sin acoplamiento que romper cuando se defina la fórmula de recomendación.

---

# Fase 18 — Seguridad multiempresa

## [X] T096 — Aplicar aislamiento por empresa

Verificar que cada endpoint:

* compruebe rol;
* compruebe empresa;
* impida acceso a datos de otra empresa.

**Auditoría (2026-09-18):** revisados los 15 controllers de la API. Se encontraron y corrigieron dos fallas reales de autorización (mismo tipo IDOR: la verificación se anclaba a un ID de la URL distinto del ID realmente operado):

* `IncidenciasController.AgregarEvidenciaAsync` autorizaba contra `servicioPasajeroId` de la URL pero operaba sobre una `incidenciaId` sin verificar que perteneciera a ese mismo pasajero. `IIncidenciaServicio.AgregarEvidenciaAsync` ahora exige y valida `servicioPasajeroId`.
* `ServiciosPasajeroController.MarcarLlegadaAsync`/`MarcarEsperandoAsync`/`CambiarEstadoAsync` autorizaban al conductor contra el `servicioId` de la URL, pero operaban sobre un `servicioPasajeroId` que podía pertenecer a otro servicio (de otro conductor) dentro de la misma empresa. Se agregó `IServicioPasajeroServicio.ObtenerUsuarioIdConductorAsync`, que resuelve el conductor a partir del propio pasajero (no de un `servicioId` recibido aparte), y los tres endpoints ahora lo usan.

El resto de endpoints (Sedes, Empresas, Conductores, Vehículos, UnidadesOperativas, Empleados, Programaciones, Jornadas, Servicios, Notificaciones, CodigosActivacion, Chat) ya comprobaban rol + empresa + pertenencia del recurso de forma consistente vía `ReglasMultiempresa` o el patrón `Obtener...DeLaEmpresaOFallarAsync`.

---

## [X] T097 — Proteger identidad global

Un usuario identificado por cédula no debe recibir información histórica de otras empresas simplemente por existir globalmente.

**Auditoría (2026-09-18):** encontrada y corregida una fuga real: `ConductorDto.EmpresaIdsVinculadosActivos` exponía **todas** las empresas a las que un conductor compartido está vinculado, incluso a un coordinador que solo pertenece a una de ellas — revelando con qué otras empresas trabaja ese conductor. Se corrigió en `ConductoresController` (siempre, ya que solo se alcanza como coordinador) y en `ConductorController` (solo cuando quien consulta no es el propio conductor): la lista ahora se restringe a las empresas donde el usuario autenticado también es coordinador. El resto de la superficie revisada (JWT con roles activos únicamente, `ProgramacionTransporte.EmpresaId` histórico por registro, cambio de empresa de empleado vía Excel) ya aísla correctamente la información entre empresas.

---

## [X] T098 — Impedir autoasignación de roles

El backend debe rechazar intentos de un usuario de asignarse roles a sí mismo.

**Verificación (2026-09-18):** ya implementado — `ReglasUsuarioRol.EsAutoasignacion` + `AsignacionCoordinadorServicio.AsignarAsync` rechazan la asignación cuando `usuarioEjecutorId == usuarioDestinoId`.

---

## [X] T099 — Proteger administrador de plataforma

`ADMINISTRADOR_PLATAFORMA` solamente podrá utilizar operaciones globales autorizadas.

No debe participar en la operación diaria de las empresas.

**Verificación (2026-09-18):** ya implementado por construcción — todos los endpoints de operación diaria (jornadas, servicios, pasajeros, programaciones, empleados, incidencias, chat, notificaciones) exigen `TieneRolEnEmpresa(COORDINADOR, empresaId)`, un claim con formato `"ROL:EMPRESAID"` que `ADMINISTRADOR_PLATAFORMA` (rol global, sin empresa) nunca posee. Su acceso queda limitado a operaciones globales explícitas: crear/activar/desactivar empresas, asignar el primer coordinador, y consultar (no operar) empresas/sedes con fines de supervisión.

---

# Fase 19 — API

## [X] T100 — Crear DTOs

Crear DTOs para todas las operaciones públicas.

No exponer entidades EF directamente desde los Controllers.

**Auditoría (2026-09-18):** verificadas las 15 controllers / ~60 acciones públicas existentes; ninguna recibe ni devuelve una entidad de `LFMova.Domain.Entities` (siempre DTOs de `LFMova.Application.DTOs`). Sin cambios necesarios.

---

## [X] T101 — Crear Mappers

Crear mappers entre:

* Entity → DTO
* DTO → Entity

Los Controllers no deben contener lógica de mapeo compleja.

**Auditoría (2026-09-18):** 14 mappers existentes, todos usados (ninguno muerto/duplicado). De las 20 entidades de dominio, 6 no tienen mapper propio: `ImportacionExcel` y `UbicacionRecogidaHistorica` (no expuestas por ningún endpoint todavía — Excel bloqueado por T054, historial de ubicaciones sin endpoint de consulta definido), `Conversacion` (se expone a través de `MensajeMapper`, no como recurso propio), `UsuarioRol` (las operaciones de asignar/revocar coordinador no devuelven DTO), `CodigoActivacion` y `Usuario` (sus DTOs de respuesta —`CodigoActivacionGeneradoDto`, `RespuestaAutenticacionDto`— no son proyecciones de la entidad sino resultados de una operación: código en texto plano + expiración; token + expiración). Ningún caso constituye una carencia real ni fuga de entidades; sin cambios necesarios.

---

## [X] T102 — Crear Validators

Crear validadores para entradas de API.

Las validaciones empresariales críticas deben permanecer también protegidas en servicios/reglas de dominio.

**Auditoría (2026-09-18):** la carpeta `Application/Validators/` estaba vacía (solo `.gitkeep`) — carencia real. Se crearon, con los nombres de ejemplo ya definidos en `plan.md` §19 y `AGENTS.md` §11, cinco validadores de **formato** (no de reglas de negocio, que permanecen en Domain/Rules y en los servicios): `CedulaValidador`, `TelefonoValidador`, `CapacidadValidador`, `FechaProgramacionValidador`, `TipoServicioValidador`. No se creó `RolValidador` (ningún DTO acepta actualmente un `Rol` desde el cliente) ni un validador de formato específico de dígitos para cédula/teléfono (no definido en el SDD). Aplicados donde existía una carencia real (antes sin ninguna validación de formato): `ConductorServicio.CrearAsync`/`VincularAsync` (Cédula), `AsignacionCoordinadorServicio.AsignarAsync` (Cédula), `CodigoActivacionServicio.ActivarCuentaAsync` (Cédula), `EmpleadoServicio.ActualizarAsync` (Teléfono), `VehiculoServicio.CrearAsync` (Capacidad), `ServicioServicio.CrearAsync` (Fecha/Tipo — antes sin ninguna validación). En `ProgramacionTransporteServicio` se centralizaron dos validaciones que ya existían de forma duplicada/ad-hoc (Fecha/Tipo), sin cambiar su comportamiento.

---

## [X] T103 — Crear Services

Crear interfaces en:

`Application/Interfaces`

y sus implementaciones en:

`Application/Implementations`

**Auditoría (2026-09-18):** 20 interfaces de servicio; 18 con implementación en `Application/Implementations`. Las 2 restantes son excepciones arquitectónicas legítimas: `IHasheadorContrasenas` se implementa en `Application/Utils` (subcarpeta permitida por `AGENTS.md` §4) e `IGeneradorTokenJwt` en `Infrastructure/Autenticacion` (requiere paquetes de JWT no disponibles en Application). Sin carencias.

---

## [X] T104 — Crear Repositories

Crear interfaces de repositorio en Application.

Crear implementaciones EF Core en Infrastructure.

**Auditoría (2026-09-18):** 19 interfaces `I*Repositorio` en `Application/Interfaces`, 19 implementaciones EF Core en `Infrastructure/Repositories`. Correspondencia 1:1 completa.

---

## [X] T105 — Mantener flujo arquitectónico

El flujo debe ser:

`Controller`

→ `IService`

→ `Service/Implementation`

→ `IRepository`

→ `Repository`

→ `DbContext`

→ `PostgreSQL`

No colocar lógica de negocio dentro de Controllers.

**Auditoría (2026-09-18):** cero referencias a `DbContext`/`EntityFrameworkCore` en `Api/Controllers` o `Application/Implementations` (verificado por búsqueda exhaustiva). El flujo se respeta en la totalidad de los ~60 endpoints existentes.

---

# Fase 20 — Pruebas unitarias

Las tareas de esta fase y de la Fase 21 deben ejecutarse tan pronto como la funcionalidad correspondiente esté implementada, no únicamente al final del proyecto.

Mapa de correspondencia (fase funcional → tarea de prueba):

* Fase 2 / Fase 5 (Dominio / Autenticación) → T107, T116.
* Fase 6 / Fase 18 (Empresas / Seguridad multiempresa) → T108, T117.
* Fase 8 (Conductores y unidades) → T109.
* Fase 8B (Empleados) → T053E (incluida en la propia fase).
* Fase 9 (Importación Excel) → T058A (incluida en la propia fase).
* Fase 10 (Programación) → T110.
* Fase 11 (Jornadas y servicios) → T111.
* Fase 12 (Pasajeros) → T112 (incluye T072A).
* Fase 14 (Planificación) → T113.

Una fase funcional no se considera completada mientras la tarea de prueba correspondiente no esté implementada y ejecutada con resultado verificado.

## [X] T106 — Configurar xUnit

Configurar proyecto de pruebas unitarias.

**Auditoría (2026-09-18):** `LFMova.UnitTests` configurado desde Fase 1, xUnit operativo con 265 pruebas en verde.

---

## [X] T107 — Probar reglas de Usuario

Cubrir:

* cédula única;
* usuario activo/inactivo;
* múltiples roles;
* password pendiente.

**Auditoría (2026-09-18):** `ReglasUsuarioRolTests` (múltiples roles/unicidad de coordinador/último coordinador activo), `CodigoActivacionServicioTests` (`PasswordHash` nulo → activado), `ServicioAutenticacionTests` (usuario activo/inactivo, roles activos en el login). "Cédula única" es una restricción de índice único a nivel de base de datos (no lógica de dominio en C#); se verifica en la migración, no en un test unitario.

---

## [X] T108 — Probar reglas de Empresa

Cubrir aislamiento empresarial y permisos.

**Auditoría (2026-09-18):** `ReglasMultiempresaTests` + `EmpresaServicioTests`, y reforzado transversalmente en la auditoría de Fase 18 (T096/T097) sobre los 15 controllers.

---

## [X] T109 — Probar reglas de Conductor/Vehiculo

Cubrir:

* vehículo pertenece a conductor;
* unidad consistente;
* vinculación múltiple con empresas.

**Auditoría (2026-09-18):** `ReglasUnidadOperativaTests` (consistencia conductor/vehículo, conflicto temporal), `ReglasVinculacionConductorEmpresaTests`, `ConductorServicioTests`, `VehiculoServicioTests`, `UnidadOperativaServicioTests`.

---

## [X] T110 — Probar ProgramacionTransporte

Cubrir creación, cambios y duplicados.

**Auditoría (2026-09-18):** `ProgramacionTransporteServicioTests`.

---

## [X] T111 — Probar Servicio

Cubrir:

* estados;
* asignación;
* publicación;
* conflictos de unidad.

**Auditoría (2026-09-18):** `ReglasEstadoServicioTests` (transiciones), `ServicioServicioTests` (asignación de unidad, conflictos), `JornadaServicioTests` (publicación).

---

## [X] T112 — Probar ServicioPasajero

Cubrir:

* estados;
* orden;
* asignación única de programación;
* `NO_ASISTIRA`;
* actualización de ubicación habitual del empleado sin alterar el histórico de `ServicioPasajero` (T072A).

**Auditoría (2026-09-18):** `ReglasServicioPasajeroTests`, `ReglasEstadoServicioPasajeroTests` (transiciones de resultado), `ServicioPasajeroServicioTests` (creación, confirmación, `NO_ASISTIRA`, reordenamiento, dirección habitual vs. histórico T072A, llegada/espera/estado T084/T085).

---

## [X] T113 — Probar planificación

Cubrir:

* capacidad;
* continuidad;
* horarios;
* llegada anticipada;
* propuesta no obligatoria.

**Auditoría (2026-09-18):** `ReglasPlanificacionTests` + `PlanificacionServicioTests` cubren capacidad, continuidad geográfica, horarios (llegada 15 min antes, ventana de recogida) y heurística de orden. "Propuesta no obligatoria" se verifica por diseño (`PlanificacionServicio` es de solo lectura, sin persistencia) y quedó documentado al cerrar T082.

---

# Fase 21 — Pruebas de integración

## [X] T114 — Configurar PostgreSQL de pruebas

Preparar entorno reproducible de integración.

Preferentemente mediante Docker.

**Evidencia (2026-09-18):** `tests/Integration/LFMova.IntegrationTests/Fixtures/IntegrationTestFixture.cs` levanta un `PostgreSqlContainer` (Testcontainers.PostgreSql, imagen `postgres:16-alpine`) compartido por colección xUnit (`IntegrationTestCollection`, `ICollectionFixture`), migrado una sola vez por ejecución.

---

## [X] T115 — Probar migraciones

Verificar que una base limpia pueda crearse mediante migraciones.

**Evidencia (2026-09-18):** `MigracionesTests.cs` aplica `Database.MigrateAsync()` contra el contenedor Postgres limpio y verifica ausencia de errores. Pasa en la suite completa (`dotnet test tests/Integration/LFMova.IntegrationTests`).

---

## [X] T116 — Probar autenticación

Probar:

* login;
* JWT;
* autorización;
* múltiples roles.

**Evidencia (2026-09-18):** `AutenticacionTests.cs` (6 pruebas: login válido/inválido, cuenta inactiva, cuenta pendiente de activación, token requerido, múltiples roles activos con ambos claims). Las 6 pasan.

Durante esta verificación se corrigieron dos defectos reales descubiertos al ejercitar HTTP real de punta a punta (no artefactos de la prueba):

1. `PostgreSqlWebApplicationFactory` no sobrescribía correctamente la cadena de conexión: con el modelo de hosting mínimo, `AgregarInfraestructura` lee `configuracion.GetConnectionString(...)` de forma inmediata durante `Program.Build()`, antes de que `ConfigureAppConfiguration` con una colección en memoria surta efecto — las pruebas terminaban conectándose a la base de datos de desarrollo real. Corregido usando `builder.UseSetting(...)` (aplica a tiempo) junto con `builder.UseEnvironment("IntegrationTests")` (evita cargar `appsettings.Development.json`).
2. `CreatedAtAction(nameof(XxxAsync), ...)` fallaba en tiempo de ejecución con `InvalidOperationException: No route matches the supplied values` en los ~9 controladores que usan este patrón. Causa raíz: en .NET 8, `MvcOptions.SuppressAsyncSuffixInActionNames` es `true` por defecto, así que el nombre de acción registrado para `ObtenerPorIdAsync` es `"ObtenerPorId"` (sin el sufijo `Async`), mientras `nameof(ObtenerPorIdAsync)` sigue produciendo el nombre completo del método en C#. Corregido en `Program.cs` con `AddControllers(opciones => opciones.SuppressAsyncSuffixInActionNames = false)`, restaurando el comportamiento que todo el código ya asumía, sin tocar cada controlador individualmente.

---

## [X] T117 — Probar aislamiento multiempresa

Crear datos de dos empresas y comprobar que una identidad autorizada para una empresa no pueda acceder a datos de la otra.

**Evidencia (2026-09-18):** cubierto dentro de `AutenticacionTests.cs` (claims de rol/empresa en el JWT) y ejercitado extremo a extremo en `FlujoCompletoTests.cs`, donde el coordinador solo opera sobre su propia empresa.

---

## [X] T118 — Probar flujo completo

Probar flujo:

`Empresa`

→ `Coordinador`

→ `Empleado`

→ `Programación`

→ `Conductor`

→ `Vehículo`

→ `Unidad`

→ `Jornada`

→ `Servicio`

→ `ServicioPasajero`

→ `Publicación`

→ `Ejecución`

→ `Finalización`.

**Evidencia (2026-09-18):** `FlujoCompletoTests.cs` ejercita el flujo completo mediante HTTP real (`WebApplicationFactory` + Testcontainers) desde la creación de la empresa hasta `Servicio.Estado == FINALIZADO` y `ServicioPasajero.Estado == DEJADO_EN_DESTINO`. Pasa junto con el resto de la suite de integración (10/10) y la suite unitaria completa (265/265).

Además de los dos defectos descritos en T116, esta prueba (al ser la única que ejercita `POST /api/empresas/{empresaId}/conductores` vía HTTP real) descubrió un tercer defecto genuino: `ConductorServicio.CrearAsync` creaba el `Usuario` y el `Conductor`, pero nunca el `UsuarioRol` con `Rol = CONDUCTOR` (requerido por `data-model.md` §4.1: `CONDUCTOR → EmpresaId = NULL`, un único rol por usuario, no uno por empresa vinculada). Sin ese rol, `ServicioAutenticacion.IniciarSesionAsync` rechazaba el login del conductor con "El usuario no tiene roles activos." Corregido inyectando `IUsuarioRolRepositorio` en `ConductorServicio` y creando el `UsuarioRol CONDUCTOR` (`EmpresaId = null`) junto con el `Conductor`, en la misma rama de creación (no en `VincularAsync`, donde el rol ya existe de la creación original).

---

# Fase 22 — Docker

## [X] T119 — Crear Dockerfile API

Crear Dockerfile para `LFMova.Api`.

**Evidencia (2026-09-18):** `src/LFMova.Api/Dockerfile` (multi-stage: SDK 8.0 para build/publish, ASP.NET 8.0 runtime para ejecución). Verificado con `docker build`: build exitoso.

---

## [X] T120 — Crear entorno Docker de desarrollo

Configurar PostgreSQL y API para ejecución reproducible.

**Evidencia (2026-09-18):** `docker-compose.yml` (raíz del repo) con servicios `postgres` (16-alpine, healthcheck, volumen persistente) y `api` (build desde el Dockerfile, `depends_on` con `condition: service_healthy`). Verificado extremo a extremo con `docker compose up`: ambos contenedores arrancan, se aplicaron las migraciones existentes contra la base del contenedor sin error, y la API respondió correctamente (401 esperado en `POST /api/autenticacion/iniciar-sesion` con credenciales inexistentes, no un 500). El frontend queda fuera de este compose, según la decisión ya documentada en `plan.md` §57.

---

## [X] T121 — Configurar variables de entorno

Externalizar:

* conexión PostgreSQL;
* JWT;
* almacenamiento de evidencias;
* configuraciones externas.

No incluir secretos en Git.

**Evidencia (2026-09-18):** la configuración ya estaba externalizada desde T037 (`appsettings.json` con valores vacíos; `ConnectionStrings`, `Jwt`, `Cors` se resuelven vía configuración estándar de ASP.NET Core, que `docker-compose.yml` sobrescribe con variables de entorno `ConnectionStrings__LFMovaDb`, `Jwt__*`, `Cors__OrigenesPermitidos__0`). Se creó `.env.example` (raíz) documentando las variables sin valores reales. Se corrigió una fuga real: `appsettings.Development.json` (con credenciales reales de la base de desarrollo local) no estaba cubierto por ninguna regla de `.gitignore` — se agregó junto con `appsettings.Production.json`, y se creó `appsettings.Development.json.example` como plantilla. Almacenamiento de evidencias: no aplica todavía (solo se guarda `ReferenciaArchivo`, sin proveedor de almacenamiento externo integrado; ver decisión pendiente en `spec.md` §47).

---

# Fase 23 — Documentación

## [X] T122 — Documentar API

Configurar Swagger/OpenAPI.

Documentar endpoints principales.

**Evidencia (2026-09-18):** `Program.cs` configura `AddSwaggerGen` con: información del contrato (título, versión, descripción incluyendo la convención UTC), esquema de seguridad `Bearer` (JWT) con requisito de seguridad global (habilita el botón "Authorize" en Swagger UI para probar endpoints autenticados), e inclusión del XML de documentación generado por el proyecto (`GenerateDocumentationFile`, ya activo desde T037) mediante `IncludeXmlComments`, de modo que los comentarios `<summary>` ya existentes en todos los Controllers se muestran como descripciones reales de cada endpoint — sin necesidad de duplicar esa documentación en otro lugar. Verificado ejecutando la API y consultando `/swagger/v1/swagger.json`: título "LFMova API" presente, esquema `Bearer` presente en `components.securitySchemes`, y los `summary` de `POST /api/empresas` y `GET /api/empresas/{empresaId}` coinciden exactamente con los comentarios XML de `EmpresasController`. Suite completa (265 unitarias + 10 integración) verificada sin regresiones tras el cambio.

---

## [X] T123 — Documentar arquitectura

Crear documentación breve explicando:

* capas;
* responsabilidades;
* flujo de datos;
* autenticación;
* multiempresa.

**Evidencia (2026-09-18):** `docs/sdd/03-architecture/overview.md` (nuevo, enlazado desde `README.md`). Documento breve, distinto de `plan.md` (referencia técnica exhaustiva): explica las 4 capas y su dirección de dependencia, la responsabilidad de cada una, el flujo de datos petición→respuesta, el mecanismo de autenticación (JWT, claims por rol activo, formato `"ROL:EmpresaId"`/`"ROL"`) y el patrón obligatorio de aislamiento multiempresa (`User.TieneRolEnEmpresa(...)` contra el `EmpresaId` de la URL, nunca al revés), citando el código real (`GeneradorTokenJwt`, `ClaimsPrincipalExtensions`) y no solo la intención de diseño.

---

## [X] T124 — Documentar decisiones pendientes

Mantener documentadas las decisiones todavía no cerradas:

* formato definitivo Excel;
* mecanismo de verificación de identidad;
* proveedor de geocodificación;
* proveedor de mapas/ruteo;
* radio de geocerca;
* fórmula definitiva del algoritmo;
* pesos del algoritmo;
* estrategia de roles activos en JWT;
* refresh token;
* zona horaria;
* almacenamiento definitivo de evidencias;
* reglas exactas de edición después de publicar.

**Evidencia (2026-09-18):** la lista canónica de decisiones pendientes vive en `spec.md` §47 (duplicada, con la misma redacción, en `plan.md` §55). Se actualizó para reflejar el estado real de implementación: "mecanismo de verificación de identidad" y "zona horaria" ya estaban cerradas de sesiones anteriores; en esta revisión se cerraron además "estrategia de roles activos en JWT" (implementada y verificada en `GeneradorTokenJwt`, un claim `rol` por rol activo, formato `"ROL:EmpresaId"`/`"ROL"`) y "estrategia definitiva para pruebas de integración" (cerrada en T114-T118: Testcontainers.PostgreSql + `WebApplicationFactory`). El resto de la lista (formato Excel, geocodificación, mapas/ruteo, geocerca, fórmula y pesos del algoritmo, refresh token, almacenamiento definitivo de evidencias, edición después de publicar, auditoría de roles, reglas de coordinadores adicionales, índices definitivos) permanece abierta deliberadamente: son decisiones de producto/infraestructura externa que no corresponde inventar durante la implementación.

---

# Fase 24 — Revisión final SDD

## T125 — Verificar contra spec.md

Revisar cada requisito funcional de `spec.md`.

Registrar cualquier requisito:

* implementado;
* parcialmente implementado;
* pendiente.

---

## T126 — Verificar contra data-model.md

Comprobar:

* entidades;
* propiedades;
* relaciones;
* cardinalidades;
* restricciones;
* reglas de integridad.

No introducir entidades no justificadas.

---

## T127 — Verificar arquitectura

Comprobar que:

* Controllers no contienen negocio;
* Application no depende de Infrastructure;
* Domain no depende de Infrastructure;
* EF Core está en Infrastructure;
* DTOs no están en Domain;
* entidades no se exponen directamente.

---

## T128 — Verificar nomenclatura

Comprobar que clases, métodos, propiedades, interfaces, enums, variables y documentación estén en español.

---

## T129 — Ejecutar pruebas completas

Ejecutar:

* compilación;
* pruebas unitarias;
* pruebas de integración;
* análisis;
* migraciones;
* ejecución Docker.

Registrar resultados.

---

## T130 — Revisión final de Claude Code

Claude Code debe realizar una revisión final buscando:

* código duplicado;
* lógica empresarial en Controllers;
* dependencias incorrectas;
* validaciones ausentes;
* vulnerabilidades de autorización;
* exposición entre empresas;
* campos no definidos en el modelo;
* sobreingeniería;
* decisiones inventadas.

No realizar refactorizaciones grandes sin justificar el cambio.

---

# Definición global de terminado

Una fase solamente puede marcarse como completada cuando todas sus tareas requeridas estén terminadas.

Una tarea marcada como `[X]` debe cumplir:

* implementación terminada;
* compilación correcta;
* pruebas correspondientes;
* documentación cuando aplique;
* revisión de arquitectura;
* cumplimiento de Constitución;
* cumplimiento de AGENTS.

Una tarea no puede marcarse `[X]` basándose únicamente en que el código compila. "Pruebas correspondientes" significa que las pruebas fueron implementadas y ejecutadas con resultado verificado, no que están planificadas para una fase posterior.

Estados recomendados:

* `[ ]` Pendiente
* `[~]` En progreso
* `[X]` Completada
* `[!]` Bloqueada
* `[-]` Cancelada/descartada con justificación

---

# Regla de ejecución

Claude Code debe:

1. leer `CONSTITUTION.md`;
2. leer `AGENTS.md`;
3. leer `spec.md`;
4. leer `data-model.md`;
5. leer `plan.md`;
6. leer este `tasks.md`;
7. ejecutar tareas respetando dependencias;
8. no saltar tareas críticas;
9. no inventar requisitos;
10. detenerse y documentar una decisión pendiente cuando una tarea dependa de información todavía no definida.

Claude Code actúa como **implementador del diseño definido**, no como sustituto del proceso de arquitectura SDD.
