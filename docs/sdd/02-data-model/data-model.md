# Modelo de Datos — Plataforma de Transporte Empresarial

## 1. Propósito

Este documento define el modelo conceptual y lógico de datos de la plataforma de transporte empresarial.

La aplicación es multiempresa y permite gestionar empleados, conductores, vehículos, sedes, programaciones, jornadas, servicios, pasajeros, incidencias, evidencias, conversaciones y notificaciones.

El modelo prioriza:

* identidad única de las personas;
* múltiples roles por usuario;
* aislamiento estricto entre empresas;
* conservación de información histórica;
* separación entre identidad, perfiles funcionales y autorización;
* simplicidad;
* trazabilidad suficiente para la operación;
* compatibilidad con C#, ASP.NET Core, Entity Framework Core y PostgreSQL.

Los detalles técnicos específicos de implementación se concretarán posteriormente en `plan.md`.

---

# 2. Principio fundamental de identidad

## 2.1 Una persona = un Usuario

`Usuario` representa la identidad de una persona dentro de la plataforma.

La cédula es única globalmente y pertenece exclusivamente a `Usuario`.

No se debe utilizar la cédula como identidad duplicada en diferentes tablas.

Conceptualmente:

```text
Persona
   |
   v
Usuario
   |
   +---- Perfil Empleado
   |
   +---- Perfil Conductor
   |
   +---- Roles
```

Una misma persona puede tener simultáneamente diferentes funciones.

Por ejemplo:

```text
Usuario
  Cédula: 123456789

Roles:
  - EMPLEADO
  - CONDUCTOR
  - COORDINADOR
```

No se crean tres usuarios diferentes para representar a la misma persona.

---

# 3. Usuario

## 3.1 Entidad

### `Usuario`

Representa la identidad autenticable de una persona.

Campos:

* `UsuarioId`
* `Cedula`
* `PasswordHash`
* `Activo`

Características:

* `UsuarioId`: clave primaria.
* `Cedula`: única globalmente.
* `PasswordHash`: puede ser `NULL` cuando la cuenta todavía no ha sido activada.
* `Activo`: permite bloquear/desactivar la cuenta sin eliminarla.

La contraseña nunca se almacena en texto plano.

---

# 4. Roles

## 4.1 UsuarioRol

### `UsuarioRol`

Representa un rol que una persona tiene dentro de una empresa.

Campos:

* `UsuarioRolId`
* `UsuarioId`
* `Rol`
* `EmpresaId`
* `Activo`

El rol no pertenece directamente a `Usuario`, porque un usuario puede tener múltiples roles.

`UsuarioRol.EmpresaId` sigue la siguiente regla, según el rol:

* `ADMINISTRADOR_PLATAFORMA` → `EmpresaId = NULL` (rol global).
* `COORDINADOR` → `EmpresaId` obligatorio.
* `EMPLEADO` → `EmpresaId` obligatorio.
* `CONDUCTOR` → `EmpresaId = NULL`. La relación de un conductor con cada empresa se resuelve exclusivamente mediante `VinculacionConductorEmpresa` (ver §10 y `AGENTS.md` §14), no mediante `UsuarioRol.EmpresaId`. No se crea un `UsuarioRol CONDUCTOR` por cada empresa vinculada.

Ejemplo:

```text
UsuarioId | Rol          | EmpresaId
------------------------------------
25        | EMPLEADO     | 1
25        | CONDUCTOR    | NULL
25        | COORDINADOR  | 1
```

## 4.2 Roles disponibles

Los roles iniciales son:

* `ADMINISTRADOR_PLATAFORMA`
* `COORDINADOR`
* `CONDUCTOR`
* `EMPLEADO`

## 4.3 Ámbito de los roles

### `ADMINISTRADOR_PLATAFORMA`

Es un rol global de plataforma.

No pertenece a una empresa concreta.

Su función principal es:

* crear empresas;
* realizar la configuración inicial de una empresa;
* asignar el primer coordinador de cada empresa.

Una vez configurada una empresa, el administrador de plataforma no participa en su operación cotidiana.

### `COORDINADOR`

Es un rol perteneciente a una única empresa. Un usuario no puede tener más de un `UsuarioRol` activo con `Rol = COORDINADOR` para empresas diferentes.

Puede:

* gestionar empleados;
* gestionar conductores;
* gestionar vehículos;
* gestionar unidades operativas;
* gestionar sedes;
* importar archivos Excel;
* crear y modificar programaciones;
* organizar jornadas;
* crear y modificar servicios;
* asignar unidades;
* publicar servicios;
* gestionar la operación de su empresa;
* asignar el rol `COORDINADOR` a otros usuarios de su propia empresa.

No puede:

* crear empresas;
* administrar otras empresas;
* asignarse a sí mismo nuevos privilegios.

### `CONDUCTOR`

Permite acceder a la operación de conducción de los servicios que correspondan al usuario.

El conductor puede trabajar con varias empresas mediante `VinculacionConductorEmpresa`.

### `EMPLEADO`

Permite acceder únicamente a la información y operaciones de transporte correspondientes al propio empleado.

---

# 5. Reglas de asignación de roles

## 5.1 El usuario no elige libremente su rol

Una persona no puede registrarse seleccionando:

```text
COORDINADOR
CONDUCTOR
EMPLEADO
```

como mecanismo para obtener permisos.

Los roles son asignados por procesos autorizados.

## 5.2 Primer coordinador de una empresa

Cuando se crea una nueva empresa:

1. El `ADMINISTRADOR_PLATAFORMA` crea la empresa.
2. El `ADMINISTRADOR_PLATAFORMA` crea o identifica al usuario correspondiente.
3. El `ADMINISTRADOR_PLATAFORMA` asigna el primer rol `COORDINADOR`.
4. El coordinador pasa a gestionar la operación de esa empresa.

El administrador de plataforma no participa posteriormente en la operación normal de la empresa.

## 5.3 Coordinadores adicionales

Un coordinador puede asignar el rol `COORDINADOR` a otro usuario de su propia empresa.

El usuario receptor puede ser:

* empleado;
* conductor;
* empleado y conductor;
* usuario previamente existente.

No se crea una segunda identidad para la persona.

Ejemplo:

```text
Usuario Juan
    |
    +-- EMPLEADO
    |
    +-- COORDINADOR
```

Si además conduce:

```text
Usuario Juan
    |
    +-- EMPLEADO
    +-- CONDUCTOR
    +-- COORDINADOR
```

## 5.4 No autoasignación

Un usuario no puede otorgarse a sí mismo el rol `COORDINADOR`.

El backend debe validar siempre que la operación de asignación sea realizada por un usuario autorizado.

## 5.5 Unicidad de empresa para el rol COORDINADOR

Un mismo `UsuarioId` no puede tener más de un `UsuarioRol` activo con `Rol = COORDINADOR` para empresas diferentes.

Esta restricción se aplica únicamente al rol `COORDINADOR`. No afecta la posibilidad de que la misma persona tenga los perfiles `Empleado` y/o `Conductor`, ni otros roles, en distintos contextos.

El backend debe validar esta restricción antes de asignar el rol.

## 5.6 Mínimo de coordinadores por empresa

Una empresa debe conservar siempre al menos un `UsuarioRol COORDINADOR` activo.

No se permite desactivar/revocar el último `UsuarioRol COORDINADOR` activo de una empresa.

El backend debe validar esta restricción antes de ejecutar cualquier revocación de rol `COORDINADOR`.

---

# 6. Empresa

## 6.1 Entidad

### `Empresa`

Representa una empresa cliente de la plataforma.

Campos:

* `EmpresaId`
* `Nombre`
* `Activa`

Reglas:

* una empresa puede tener múltiples sedes;
* una empresa puede tener múltiples empleados;
* una empresa puede tener múltiples coordinadores, pero debe conservar siempre al menos uno activo (ver §5.6);
* una empresa puede vincular múltiples conductores;
* una empresa inactiva conserva su información histórica;
* solamente `ADMINISTRADOR_PLATAFORMA` puede crear empresas.

---

# 7. Sede

## 7.1 Entidad

### `Sede`

Representa una ubicación física de una empresa.

Campos:

* `SedeId`
* `EmpresaId`
* `Nombre`
* `Direccion`
* `Ciudad`
* `Barrio`
* `Latitud`
* `Longitud`
* `Activa`

Reglas:

* una empresa puede tener múltiples sedes;
* cada sede pertenece a una única empresa;
* una sede inactiva puede conservarse para información histórica;
* una sede inactiva no debe ofrecerse para nueva programación;
* las coordenadas pueden ser `NULL`;
* los algoritmos de planificación utilizan coordenadas cuando están disponibles;
* ciudad y barrio son datos descriptivos y no deben utilizarse como listas geográficas codificadas.

Cada servicio está asociado a exactamente una sede.

---

# 8. Empleado

## 8.1 Entidad

### `Empleado`

Representa el perfil laboral/operativo de empleado asociado a un `Usuario`.

Campos:

* `EmpleadoId`
* `UsuarioId`
* `EmpresaId`
* `NombreCompleto`
* `Telefono`
* `Direccion`
* `Barrio`
* `Activo`

Reglas:

* `UsuarioId` referencia al usuario que representa a la persona;
* una persona no debe tener múltiples identidades para representar diferentes funciones;
* el empleado pertenece actualmente a una empresa;
* una cédula no se duplica para crear otro empleado;
* un empleado puede cambiar de empresa; el cambio se detecta y se ejecuta automáticamente durante la importación Excel al encontrar la misma `Cedula` en otra empresa (ver §13.2 y §27); no existe una operación manual de transferencia;
* el cambio de empresa actualiza el `EmpresaId` del perfil;
* los registros históricos permanecen vinculados a la empresa donde fueron generados;
* no se elimina físicamente un empleado con información histórica.

---

# 9. Conductor

## 9.1 Entidad

### `Conductor`

Representa el perfil de conductor asociado a un `Usuario`.

Campos:

* `ConductorId`
* `UsuarioId`
* `NombreCompleto`
* `Telefono`
* `Activo`

Reglas:

* `UsuarioId` referencia al usuario que representa a la persona;
* un conductor puede trabajar para múltiples empresas;
* la relación con cada empresa se gestiona mediante `VinculacionConductorEmpresa`;
* `Activo` representa el estado global del conductor;
* no se elimina físicamente un conductor con información histórica.

---

# 10. Vinculación de conductor con empresa

## 10.1 Entidad

### `VinculacionConductorEmpresa`

Representa la autorización de un conductor para trabajar con una empresa determinada.

Campos:

* `VinculacionConductorEmpresaId`
* `ConductorId`
* `EmpresaId`
* `Activa`

Restricción:

```text
UNIQUE(ConductorId, EmpresaId)
```

Un conductor puede estar:

```text
Conductor Carlos
    |
    +-- Empresa A → Activa
    +-- Empresa B → Activa
    +-- Empresa C → Inactiva
```

El estado global del conductor y su estado de vinculación con una empresa son conceptos independientes.

---

# 11. Vehículo

## 11.1 Entidad

### `Vehiculo`

Representa un vehículo perteneciente exclusivamente a un conductor.

Campos:

* `VehiculoId`
* `ConductorId`
* `Placa`
* `Marca`
* `Modelo`
* `Capacidad`
* `Activo`

Reglas:

* un conductor puede tener múltiples vehículos;
* un vehículo pertenece exclusivamente a un conductor;
* un vehículo no puede ser utilizado temporalmente por otro conductor;
* `Placa` es única;
* un vehículo histórico se desactiva, no se elimina físicamente.

---

# 12. Unidad operativa

## 12.1 Entidad

### `UnidadOperativa`

Representa la combinación operacional entre un conductor y un vehículo.

Campos:

* `UnidadOperativaId`
* `ConductorId`
* `VehiculoId`
* `Activa`

Restricciones:

```text
UNIQUE(VehiculoId)
```

Regla fundamental:

```text
UnidadOperativa.ConductorId
==
Vehiculo.ConductorId
```

La unidad operativa se asigna individualmente a cada `Servicio`, no a la `Jornada`. Una `Jornada` puede así involucrar múltiples unidades operativas distintas a través de sus servicios (ver §15-16).

El coordinador puede crear conductor, vehículo y unidad operativa desde una misma operación administrativa.

## 12.2 Ciclo de vida ante cambio de vehículo

Un `Vehiculo` pertenece exclusivamente a un `Conductor` durante toda su vida útil operativa.

Un vehículo puede tener como máximo una `UnidadOperativa` (`UNIQUE(VehiculoId)`).

Cuando un conductor deja de utilizar un vehículo:

* el `Vehiculo` se desactiva (`Activo = false`);
* la `UnidadOperativa` asociada se desactiva (`Activa = false`).

Si el conductor comienza a operar con otro vehículo, se crea una nueva `UnidadOperativa` para la combinación conductor + vehículo nuevo. No se reutiliza ni se modifica la `UnidadOperativa` anterior.

Los `Servicio` ya creados conservan la referencia a la `UnidadOperativa` que efectivamente utilizaron. No se modifica un `Servicio` histórico para sustituir la unidad utilizada.

Los nuevos `Servicio` deben utilizar la `UnidadOperativa` activa vigente al momento de asignarse.

---

# 13. Importación de Excel

## 13.1 Entidad

### `ImportacionExcel`

Representa una importación realizada por un coordinador.

Campos:

* `ImportacionExcelId`
* `EmpresaId`
* `CoordinadorId`
* `NombreArchivo`
* `FechaImportacion`
* `Estado`

`CoordinadorId` referencia:

```text
Usuario.UsuarioId
```

No existe una entidad independiente `Coordinador`.

El coordinador es un `Usuario` que posee el rol `COORDINADOR`.

## 13.2 Reglas

* una empresa puede tener múltiples importaciones;
* una importación pertenece a una empresa;
* debe ejecutarla un usuario autorizado de esa empresa;
* una nueva importación no debe eliminar ciegamente programaciones anteriores;
* los empleados nuevos pueden ser creados automáticamente;
* la misma cédula global identifica al mismo usuario;
* si una persona (identificada por su `Cedula` mediante `Usuario`) aparece vinculada a una empresa diferente a la actual, se actualizan automáticamente, en la misma transacción, `Empleado.EmpresaId` y el `EmpresaId` del `UsuarioRol` del rol `EMPLEADO` correspondiente (ver §27);
* los registros históricos no se modifican.

El formato exacto de Excel queda pendiente hasta disponer del archivo real.

## 13.3 Estados

Valores de `Estado`:

* `PENDIENTE`
* `PROCESANDO`
* `COMPLETADA`
* `COMPLETADA_CON_ADVERTENCIAS`
* `ERROR`

---

# 14. Programación de transporte

## 14.1 Entidad

### `ProgramacionTransporte`

Representa la necesidad de transporte de un empleado para una fecha concreta.

Campos:

* `ProgramacionTransporteId`
* `EmpresaId`
* `EmpleadoId`
* `SedeId`
* `Fecha`
* `Hora`
* `Tipo`
* `DireccionRecogida`
* `BarrioRecogida`

## 14.2 Tipo

Valores:

* `ENTRADA`
* `SALIDA`

## 14.3 Reglas

Una programación:

* pertenece a una empresa;
* pertenece a un empleado;
* está asociada a una sede;
* tiene fecha y hora separadas;
* representa una necesidad de transporte concreta;
* puede permanecer temporalmente sin asignación a un servicio.

Para una entrada:

```text
Empleado → Sede
```

Para una salida:

```text
Sede → Empleado
```

`DireccionRecogida` representa el punto del empleado que participa en ese servicio.

La programación conserva `EmpresaId` porque constituye contexto histórico de la operación.

---

# 15. Jornada

## 15.1 Entidad

### `Jornada`

Representa el bloque operativo de transporte de una empresa para una fecha operativa determinada. No pertenece a una única unidad operativa: puede involucrar múltiples `UnidadOperativa` distintas, cada una asignada individualmente a través de sus `Servicio` (ver §16).

Campos:

* `JornadaId`
* `EmpresaId`
* `FechaOperativa`

Una jornada no tiene:

* `UnidadOperativaId`;
* `HoraInicio`;
* `HoraFin`.

Ejemplo (una misma jornada, múltiples unidades):

```text
Jornada 18/09 — Empresa X

Servicio 1 → UnidadOperativa (Carlos + ABC123)
Servicio 2 → UnidadOperativa (Carlos + ABC123)
Servicio 3 → UnidadOperativa (Pedro + XYZ456)
Servicio 4 → UnidadOperativa (Luis + DEF789)
```

La relación entre `Jornada` y `UnidadOperativa` es indirecta: se produce únicamente a través de los `Servicio` que pertenecen a esa jornada.

## 15.2 Fecha operativa

`FechaOperativa` representa la fecha de referencia del bloque operacional.

No necesariamente coincide con la fecha calendario de todos los servicios contenidos.

Ejemplo:

```text
Jornada
FechaOperativa = 18/09

Servicio 1
17/09 - 22:00 - SALIDA

Servicio 2
18/09 - 00:00 - ENTRADA

Servicio 3
18/09 - 04:00 - ENTRADA
```

Esto permite representar operaciones que cruzan medianoche.

No existe una restricción única sobre `EmpresaId + FechaOperativa`: pueden existir múltiples jornadas de una misma empresa con la misma fecha de referencia si la operación lo requiere.

---

# 16. Servicio

## 16.1 Entidad

### `Servicio`

Representa una operación concreta de transporte.

Campos:

* `ServicioId`
* `JornadaId`
* `UnidadOperativaId` (opcional/nulo mientras el servicio no tenga unidad asignada)
* `SedeId`
* `Fecha`
* `HoraProgramada`
* `Tipo`
* `Estado`
* `HoraInicioReal`
* `HoraFinReal`

No contiene:

* `EmpresaId`.

`UnidadOperativaId` pertenece a `Servicio`, no a `Jornada`. La asignación de unidad operativa es individual por servicio: dos servicios de la misma jornada pueden tener unidades operativas distintas.

La empresa se obtiene mediante:

```text
Servicio
   ↓
Jornada
   ↓
Empresa
```

La unidad operativa se obtiene directamente:

```text
Servicio.UnidadOperativaId
```

sin pasar por la jornada.

## 16.2 Estados

* `BORRADOR`
* `PENDIENTE_ASIGNACION`
* `ASIGNADO`
* `PUBLICADO`
* `EN_CURSO`
* `FINALIZADO`
* `CANCELADO`

## 16.3 Reglas

* cada servicio pertenece a una jornada;
* cada servicio tiene exactamente una sede;
* jornada, servicio y sede deben pertenecer a la misma empresa;
* la unidad operativa de un servicio debe pertenecer, mediante su conductor, a una vinculación activa con esa misma empresa;
* un servicio puede estar temporalmente sin unidad durante reorganización (`UnidadOperativaId = NULL`);
* un servicio sin resolver no puede publicarse;
* antes de publicar, todos los servicios deben estar asignados a una unidad o resolverse mediante reasignación/eliminación;
* el coordinador puede redistribuir servicios manualmente entre unidades operativas;
* reasignar la unidad operativa de un servicio modifica únicamente `Servicio.UnidadOperativaId`; `Servicio.JornadaId` no cambia;
* el sistema debe bloquear únicamente conflictos incompatibles de horario/asignación (una misma unidad no puede atender dos servicios simultáneos incompatibles);
* distancia, carga de trabajo o calidad de ruta no constituyen por sí mismas bloqueos de asignación; el algoritmo puede proponer o advertir, pero no impedir una reasignación manual válida del coordinador.

---

# 17. Servicio pasajero

## 17.1 Entidad

### `ServicioPasajero`

Representa la participación de un empleado concreto en un servicio.

Campos:

* `ServicioPasajeroId`
* `ServicioId`
* `ProgramacionTransporteId`
* `EmpleadoId`
* `Estado`
* `Orden`
* `DireccionRecogida`
* `Latitud`
* `Longitud`
* `LatitudCompartida` (opcional)
* `LongitudCompartida` (opcional)
* `FechaHoraUbicacionCompartida` (opcional, UTC)

**Decisión del usuario (2026-10-01):** `LatitudCompartida`/`LongitudCompartida`/`FechaHoraUbicacionCompartida` guardan el punto que el propio empleado compartió para ese servicio y cuándo lo hizo. `Latitud`/`Longitud` siguen siendo el punto de recogida utilizado (también los escribe el conductor al guardar la ubicación), por lo que no sirven para saber quién lo puso. Solo se conserva la última ubicación compartida, sin historial (ver `AGENTS.md` §18).

Restricción:

```text
UNIQUE(ProgramacionTransporteId)
```

Una programación puede estar asociada como máximo a un servicio pasajero real.

## 17.2 Estados

* `PROGRAMADO`
* `CONFIRMADO`
* `NO_ASISTIRA`
* `CONDUCTOR_LLEGO`
* `RECOGIDO`
* `NO_RECOGIDO`
* `CANCELADO`

**Decisión del usuario (2026-09-19):** el conductor solo registra dos pasos por pasajero: la llegada al punto de recogida (`CONDUCTOR_LLEGO`) y el resultado final (`RECOGIDO` o el tipo de incidencia). Se eliminaron `ESPERANDO`, `EN_VEHICULO` y `DEJADO_EN_DESTINO`: un pasajero `RECOGIDO` se considera transportado cuando el servicio se finaliza, sin acción adicional del conductor.

**Decisión del usuario (2026-09-19, ajuste):** los motivos por los que no se recoge a un pasajero (`NO_CONTESTA`, `NO_SE_ENCUENTRA`, `DIRECCION_INCORRECTA`, `NO_SE_PUDO_RECOGER`) ya no son estados: se registran como **incidencias** (con foto y ubicación). Una incidencia de esos cuatro tipos deja al pasajero automáticamente como `NO_RECOGIDO`. `UBICACION_MODIFICADA` y `OTRA` son informativas y no cambian el estado.

## 17.3 Reglas

* conserva la dirección exacta utilizada para ese servicio;
* puede conservar coordenadas específicas;
* el conductor puede modificar el orden de recogida;
* `Orden` almacena el orden operativo actual;
* no existe historial automático de cambios de estado;
* el estado actual es suficiente para la operación inicial;
* `NO_ASISTIRA` no bloquea la finalización del servicio;
* un servicio puede permanecer temporalmente sin pasajeros durante reorganización;
* un servicio vacío o sin resolver no puede publicarse.

---

# 18. Ubicación de recogida histórica

## 18.1 Entidad

### `UbicacionRecogidaHistorica`

Representa una ubicación anteriormente utilizada por un empleado.

Campos:

* `UbicacionRecogidaHistoricaId`
* `EmpleadoId`
* `Direccion`
* `Barrio`
* `Latitud`
* `Longitud`
* `FechaRegistro`

`FechaRegistro` almacena el momento en que la ubicación fue registrada/utilizada, y permite determinar cuál es la ubicación histórica más reciente.

Un empleado puede tener múltiples ubicaciones históricas.

La información se diferencia de:

### `Empleado`

Dirección actual/default.

### `UbicacionRecogidaHistorica`

Ubicaciones utilizadas anteriormente.

### `ServicioPasajero`

Ubicación exacta utilizada en un servicio específico.

---

# 19. Incidencia

## 19.1 Entidad

### `Incidencia`

Representa un problema ocurrido durante la operación de un pasajero.

Campos:

* `IncidenciaId`
* `ServicioPasajeroId`
* `Tipo`
* `Descripcion`
* `FechaHora`
* `Latitud`
* `Longitud`

## 19.2 Tipos

* `NO_CONTESTA`
* `NO_SE_ENCUENTRA`
* `DIRECCION_INCORRECTA`
* `NO_SE_PUDO_RECOGER`
* `UBICACION_MODIFICADA`
* `OTRA`

Un servicio pasajero puede tener múltiples incidencias.

---

# 20. Evidencia

## 20.1 Entidad

### `Evidencia`

Representa una evidencia asociada a una incidencia.

Campos:

* `EvidenciaId`
* `IncidenciaId`
* `Tipo`
* `ReferenciaArchivo`
* `FechaHora`

La evidencia no almacena el archivo binario directamente en PostgreSQL.

`ReferenciaArchivo` contiene la referencia al almacenamiento externo que se defina técnicamente.

Una incidencia puede tener múltiples evidencias.

## 20.2 Tipos

La plataforma define inicialmente un único tipo de evidencia:

* `FOTOGRAFIA`

No se contemplan inicialmente otros tipos (por ejemplo, documentos u otros archivos). Cualquier tipo adicional requerirá una decisión explícita antes de implementarse.

---

# 21. Conversación

## 21.1 Entidad

### `Conversacion`

Representa una conversación privada entre conductor y empleado relacionada con un servicio pasajero.

Campos:

* `ConversacionId`
* `ServicioPasajeroId`

Restricción:

```text
UNIQUE(ServicioPasajeroId)
```

Un servicio pasajero puede tener cero o una conversación.

No se implementa chat grupal en esta versión.

---

# 22. Mensaje

## 22.1 Entidad

### `Mensaje`

Representa un mensaje individual dentro de una conversación.

Campos:

* `MensajeId`
* `ConversacionId`
* `UsuarioId`
* `Contenido`
* `FechaHora`

Una conversación contiene múltiples mensajes.

El remitente se identifica mediante `UsuarioId`.

La aplicación no almacena grabaciones de llamadas.

---

# 23. Notificación

## 23.1 Entidad

### `Notificacion`

Representa una notificación dirigida a un usuario.

Campos:

* `NotificacionId`
* `UsuarioId`
* `Tipo`
* `Titulo`
* `Mensaje`
* `Leida`
* `FechaHora`

Un usuario puede recibir múltiples notificaciones.

Las modificaciones de una programación deben notificar únicamente a los usuarios directamente afectados cuando corresponda.

Ejemplo:

```text
Juan pasa de conductor Carlos a conductor Pedro.

Notificar:
✓ Juan
✓ Pedro

No notificar necesariamente:
✗ Carlos
```

---

# 24. Relaciones principales

```text
Empresa
  ├── 1:N Sede
  ├── 1:N Empleado
  ├── 1:N ImportacionExcel
  ├── 1:N ProgramacionTransporte
  ├── 1:N Jornada
  └── N:M Conductor
          mediante VinculacionConductorEmpresa
```

```text
Usuario
  ├── 1:N UsuarioRol
  ├── 1:0..1 Empleado
  ├── 1:0..1 Conductor
  ├── 1:N Mensaje
  ├── 1:N Notificacion
  └── 1:N CodigoActivacion
```

```text
Empleado
  ├── 1:N ProgramacionTransporte
  ├── 1:N ServicioPasajero
  └── 1:N UbicacionRecogidaHistorica
```

```text
Conductor
  ├── 1:N Vehiculo
  ├── 1:N UnidadOperativa
  ├── N:M Empresa
  └── 1:N CodigoActivacion
      (como generador)
```

```text
Vehiculo
  └── 1:1 UnidadOperativa
```

```text
UnidadOperativa
  └── 1:N Servicio
```

```text
Jornada
  └── 1:N Servicio
```

```text
Servicio
  ├── N:1 Jornada
  ├── N:0..1 UnidadOperativa
  ├── N:1 Sede
  └── 1:N ServicioPasajero
```

```text
ProgramacionTransporte
  └── 1:0..1 ServicioPasajero
```

```text
ServicioPasajero
  ├── N:1 Servicio
  ├── N:1 Empleado
  ├── 1:N Incidencia
  └── 1:0..1 Conversacion
```

```text
Incidencia
  └── 1:N Evidencia
```

```text
Conversacion
  └── 1:N Mensaje
```

---

# 25. Integridad multiempresa

El backend debe garantizar que las relaciones entre entidades pertenecientes a empresas diferentes no puedan utilizarse para cruzar información indebidamente.

Ejemplos:

```text
Servicio → Jornada → Empresa A
Servicio → Sede → Empresa A
Servicio → UnidadOperativa → Conductor → vinculación activa con Empresa A
```

es válido.

Pero:

```text
Servicio → Jornada → Empresa A
Servicio → Sede → Empresa B
```

debe rechazarse, y también:

```text
Servicio → Jornada → Empresa A
Servicio → UnidadOperativa → Conductor → vinculación activa únicamente con Empresa B
```

debe rechazarse.

También deben validarse:

* empleado y programación;
* programación y empresa;
* sede y empresa;
* servicio y unidad operativa (mediante la vinculación conductor-empresa);
* conductor y vinculación empresarial;
* coordinador y empresa;
* usuario y roles;
* acceso a servicios publicados.

La seguridad multiempresa debe aplicarse en backend, independientemente de las restricciones visuales del frontend.

---

# 26. Integridad de identidad

Debe cumplirse:

```text
Usuario.Cedula
```

es la única identidad global de la persona.

No deben existir diferentes usuarios con la misma cédula.

Los perfiles funcionales utilizan:

```text
UsuarioId
```

como referencia.

Por tanto:

```text
Usuario
   |
   +── Empleado
   |
   +── Conductor
```

representa a la misma persona cuando corresponda.

Una persona puede tener ambos perfiles simultáneamente.

---

# 27. Cambio de empresa de un empleado

Una persona empleada puede cambiar de empresa. No existe una operación manual de transferencia; el cambio se detecta y se ejecuta automáticamente durante la importación de un archivo Excel, cuando la misma `Cedula` (identificada vía `Usuario`) aparece en la importación de una empresa distinta a la actual.

`ADMINISTRADOR_PLATAFORMA` no participa en este proceso ni en la gestión operativa de empleados.

Un `COORDINADOR` únicamente gestiona empleados de su propia empresa; no puede iniciar ni ejecutar el cambio de empresa de un empleado hacia o desde otra empresa.

Al detectarse el cambio, deben actualizarse en una misma transacción:

* `Empleado.EmpresaId`
* `UsuarioRol` del rol `EMPLEADO` correspondiente (su `EmpresaId`)

El sistema mantiene un único:

```text
Usuario
```

y un único perfil:

```text
Empleado
```

El cambio modifica su empresa actual.

Los registros históricos no se trasladan ni se modifican.

Ejemplo:

```text
2026
Empleado Juan
Empresa A

ProgramacionTransporte histórica
→ Empresa A

2027
Empleado Juan
Empresa B

Nueva ProgramacionTransporte
→ Empresa B
```

No se crea un segundo usuario ni un segundo empleado para Juan.

La plataforma no debe exponer información histórica de Empresa A a Empresa B únicamente por compartir la misma cédula.

---

# 28. Activación de cuentas

> **REVISADO 2026-09-19.** las cuentas se crean por **autorregistro abierto** (correo, nombre completo, cédula, teléfono y contraseña) y la identidad se confirma por **enlace enviado al correo** (`TokenVerificacion`, proveedor Brevo). Una cuenta nueva recibe el rol `EMPLEADO` sin empresa (`UsuarioRol.EmpresaId = NULL`) y no puede iniciar sesión hasta confirmar su correo. Solo hay dos casos de cuenta creada por invitación, ambos sin contraseña escrita por terceros: el primer coordinador que crea el administrador de plataforma y los coordinadores adicionales; reciben un correo para establecer su propia contraseña. Un conductor es una persona ya registrada a la que un coordinador busca por cédula y a la que asigna el rol `CONDUCTOR`; nunca se crea una cuenta desde ese flujo. El inicio de sesión acepta cédula o correo. Existe recuperación de contraseña por correo. La entidad `CodigoActivacion` y su flujo (código generado por un conductor) se eliminaron por completo. Modelo vigente: `Usuario` gana `Email` (único), `NombreCompleto`, `Telefono` y `CorreoConfirmado`; `TokenVerificacion` (`Tipo`: `CONFIRMACION_CORREO` | `ESTABLECER_CONTRASENA`, `TokenHash`, `FechaExpiracion`, `Utilizado`) reemplaza a `CodigoActivacion`. Las subsecciones 28.1 a 28.x siguientes describen el mecanismo anterior y se conservan solo como histórico.

Los empleados pueden ser creados automáticamente a partir de una importación Excel.

La cuenta puede comenzar sin contraseña:

```text
PasswordHash = NULL
```

Esto significa que la cuenta todavía no ha completado su activación inicial.

## 28.1 Mecanismo de activación (decisión cerrada)

La activación inicial se realiza mediante un **código temporal generado exclusivamente por un conductor**, nunca por el coordinador ni por el administrador de plataforma, y sin ningún proveedor externo (no WhatsApp, no SMS, no email).

Un conductor solo puede generar el código de activación de un empleado con el que tenga una relación operativa real: el empleado debe estar asignado (mediante `ServicioPasajero.EmpleadoId`) a algún `Servicio` cuya `UnidadOperativa` pertenezca a ese conductor. Esa relación constituye por sí misma la autorización; no se requiere ninguna verificación de identidad adicional.

El conductor únicamente genera y entrega el código (presencialmente o por teléfono). El conductor no puede:

* buscar cualquier empleado por cédula sin esa relación;
* generar códigos para empleados no asignados a alguno de sus servicios;
* activar directamente la cuenta;
* cambiar la contraseña del empleado;
* modificar la identidad o los datos del empleado.

## 28.2 Entidad `CodigoActivacion`

Campos:

* `CodigoActivacionId`
* `UsuarioId` (empleado cuya cuenta se activa)
* `ConductorId` (conductor que generó el código, para trazabilidad)
* `CodigoHash` (el código nunca se almacena en texto plano; se hashea con el mismo mecanismo ya utilizado para `Usuario.PasswordHash`)
* `FechaGeneracion`
* `FechaExpiracion`
* `Utilizado` (booleano; un código es de un solo uso)
* `IntentosFallidos` (contador con límite máximo de intentos)

Reglas:

* el código es único, temporal y de un solo uso;
* se invalida (`Utilizado = true`) inmediatamente después de usarse correctamente;
* deja de ser válido al expirar o al superar el límite de intentos fallidos, aunque el registro no se elimina físicamente (ver §29);
* puede generarse un nuevo código para el mismo `Usuario` si el anterior expiró o se agotaron sus intentos; el anterior permanece invalidado.

## 28.3 Flujo

```text
Importación Excel
      ↓
Empleado (Usuario.PasswordHash = NULL)
      ↓
Empleado asignado a un Servicio de un Conductor
      ↓
Conductor genera CodigoActivacion
      ↓
Conductor entrega el código (presencial o telefónico)
      ↓
Empleado: "Activar mi cuenta"
  (Cédula + Código + Nueva contraseña + Confirmación)
      ↓
Validación:
  1. el Usuario existe y corresponde a la cédula;
  2. la cuenta está pendiente de activación (PasswordHash = NULL);
  3. el código corresponde a ese Usuario;
  4. el código no está expirado;
  5. el código no fue utilizado;
  6. no se superó el límite de intentos;
      ↓
Se genera PasswordHash, se marca el código como utilizado
      ↓
Cuenta activa (login normal)
```

El coordinador y el `ADMINISTRADOR_PLATAFORMA` no participan en ningún paso de este flujo. No se modifica ningún registro operativo histórico durante la activación.

---

# 29. Historial y eliminación

Las entidades que participan en operaciones históricas no deben eliminarse físicamente cuando su eliminación provoque pérdida de información.

Se utilizará desactivación lógica cuando corresponda.

Entidades principales que deben conservar historial:

* Empresa
* Sede
* Empleado
* Conductor
* Vehículo
* UnidadOperativa
* Usuario
* Programaciones
* Jornadas
* Servicios
* ServiciosPasajero
* Incidencias
* Evidencias
* Conversaciones
* Mensajes
* Notificaciones
* CodigosActivacion (un código utilizado, expirado o agotado se invalida; no se elimina físicamente)

Los detalles concretos de restricciones de borrado serán definidos técnicamente en `plan.md`.

---

# 30. Reglas de operación de rutas

El modelo de datos soporta un algoritmo que considere:

* capacidad del vehículo;
* horario;
* ubicación;
* continuidad geográfica;
* tiempo estimado de desplazamiento;
* un tiempo conjunto aproximado de recogida (referencia inicial: 25 minutos; valor parametrizable y no definitivo, pendiente de validación — ver §34);
* llegada a la sede al menos 15 minutos antes de la hora de entrada;
* ubicación del servicio anterior;
* ubicación del siguiente servicio;
* carga de trabajo.

Para entradas se utilizará como heurística general:

```text
pasajero más lejano
        ↓
pasajeros progresivamente más cercanos
        ↓
sede
```

El algoritmo realiza recomendaciones.

No sustituye la decisión del coordinador.

El coordinador puede modificar manualmente el orden y las asignaciones siempre que no se incumplan restricciones operativas duras.

La fórmula exacta, pesos, proveedor de mapas y cálculo de tiempos quedan pendientes para `plan.md`.

---

# 31. Ejecución del servicio

El modelo debe soportar:

### Inicio

El conductor inicia manualmente la ruta.

Se registra:

```text
HoraInicioReal
```

### Llegada al pasajero

La aplicación puede utilizar geolocalización automática o permitir una alternativa manual.

### Espera

El tiempo máximo operativo definido inicialmente es de aproximadamente 2 minutos.

### Incidencias

El conductor puede registrar incidencias y evidencias.

### Finalización

Para servicios de entrada:

* el servicio puede finalizar al llegar a la sede;
* no debe permitirse finalización manual mientras existan pasajeros pendientes de procesar;
* podrá existir finalización automática bajo reglas de geolocalización que se definirán posteriormente.

Para servicios de salida:

* pueden finalizar cuando se complete el transporte de los pasajeros;
* no necesitan terminar en una ubicación concreta.

El registro de ubicación exacta y radio de geocerca queda pendiente de definición técnica.

---

# 32. Publicación

La publicación representa el envío operativo de una jornada hacia los usuarios involucrados.

Antes de publicar:

* los servicios deben estar resueltos;
* no deben existir servicios publicados sin unidad;
* no deben existir inconsistencias entre empresa, sede, jornada y servicio.

Después de publicar:

* el coordinador puede realizar modificaciones;
* los usuarios directamente afectados deben recibir las notificaciones correspondientes.

La publicación no congela necesariamente la información para siempre.

Las reglas exactas de edición según estado serán definidas posteriormente.

---

# 33. Decisiones deliberadamente fuera del modelo inicial

No se incluyen inicialmente:

* historial completo de estados;
* `HistorialEstadoServicioPasajero`;
* historial completo de estados de `Servicio`;
* OAuth;
* chat grupal;
* grabación de llamadas;
* Event Sourcing;
* CQRS;
* microservicios;
* automatización que sustituya al coordinador;
* optimización avanzada basada en IA;
* historial independiente de cambios de empresa del empleado;
* almacenamiento de archivos binarios de evidencias en PostgreSQL.

Estas capacidades podrán evaluarse posteriormente si existe una necesidad real.

---

# 34. Aspectos técnicos pendientes para `plan.md`

Los siguientes puntos permanecen deliberadamente abiertos:

1. Longitudes exactas de campos `varchar`.
2. Precisión de `Latitud` y `Longitud`.
3. Estrategia de enums en PostgreSQL/EF Core.
4. Índices.
5. Restricciones de claves foráneas.
6. Política exacta de borrado.
7. Configuración de Entity Framework Core.
8. Migraciones.
9. Estrategia de almacenamiento de evidencias.
10. Formato exacto del Excel.
11. Proveedor de geocodificación.
12. Proveedor de mapas/rutas.
13. Radio de geocerca.
14. Fórmula y pesos del algoritmo de planificación.
15. Estrategia JWT.
16. Política de expiración y renovación de tokens.

La validación de identidad para activación ya no es una decisión pendiente: se resuelve mediante el código de activación generado por el conductor (ver §28).

La configuración de zona horaria ya no es una decisión pendiente (decisión vigente, 2026-09-30, sustituye a la del 2026-09-18 que ponía todo en UTC): la aplicación opera en hora de Colombia (UTC−5 fijo): las fechas y horas programadas (`Fecha`, `HoraProgramada`, `FechaOperativa`) se guardan y comparan en hora de Colombia, y los instantes reales (`HoraInicioReal`, `HoraFinReal`, `FechaHora`) se guardan en UTC y el frontend los muestra en hora de Colombia (ver `AGENTS.md` §41 y `plan.md` §48).
19. Estrategia de autorización para múltiples roles.
20. Estrategia para selección de rol activo cuando un usuario posee múltiples roles.
21. Auditoría de asignación y eliminación de roles.

Estos puntos deben resolverse en `plan.md` sin inventar requisitos de negocio que todavía no hayan sido aprobados.
