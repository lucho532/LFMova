# Plan de Implementación — Plataforma de Transporte Empresarial

## 1. Objetivo

Este documento define la estrategia técnica para implementar la Plataforma de Transporte Empresarial a partir de `spec.md`, `data-model.md`, `CONSTITUTION.md` y `AGENTS.md`.

El objetivo es construir una aplicación monolítica modular, mantenible y preparada para crecer, utilizando:

* C#
* ASP.NET Core Web API
* Entity Framework Core
* PostgreSQL
* JWT
* Swagger/OpenAPI
* Docker
* xUnit
* Git/GitHub

La implementación debe respetar estrictamente las decisiones de negocio ya aprobadas.

Claude Code actúa como implementador técnico.

No debe inventar requisitos funcionales ni modificar reglas de negocio sin autorización.

---

# 2. Estrategia general

La primera versión será un **monolito modular por capas**, no una arquitectura de microservicios.

Flujo principal:

```text
React
  ↓
HTTP / JSON
  ↓
Controllers
  ↓
Application Services
  ↓
Repositories
  ↓
Entity Framework Core
  ↓
PostgreSQL
```

Para devolver información:

```text
PostgreSQL
  ↓
EF Core
  ↓
Entities
  ↓
Mappers
  ↓
DTOs
  ↓
Controllers
  ↓
JSON
  ↓
React
```

Las reglas de negocio deben ejecutarse en backend.

El frontend no se considera un mecanismo de seguridad.

---

# 3. Estructura de solución

La solución utilizará una estructura similar a:

```text
LFMova/
│
├── src/
│   │
│   ├── LFMova.Api/
│   │   ├── Controllers/
│   │   ├── Middleware/
│   │   ├── Configuration/
│   │   └── Program.cs
│   │
│   ├── LFMova.Application/
│   │   ├── DTOs/
│   │   ├── Interfaces/
│   │   ├── Services/
│   │   ├── Implementations/
│   │   ├── Mappers/
│   │   ├── Validators/
│   │   └── Utils/
│   │
│   ├── LFMova.Domain/
│   │   ├── Entities/
│   │   ├── Enums/
│   │   └── Rules/
│   │
│   └── LFMova.Infrastructure/
│       ├── Data/
│       ├── Repositories/
│       ├── Configurations/
│       └── Migrations/
│
├── tests/
│   ├── Unit/
│   └── Integration/
│
├── docker/
│
├── docs/
│
├── .gitignore
├── docker-compose.yml
├── README.md
└── LFMova.sln
```

La estructura puede ajustarse si una necesidad técnica real lo justifica, pero no se deben crear proyectos o capas adicionales sin necesidad.

---

# 4. Dependencias entre capas

Las dependencias deben respetar:

```text
Api
 ↓
Application
 ↓
Domain

Infrastructure
 ↓
Application
 ↓
Domain
```

Reglas:

* `Domain` no depende de ASP.NET Core.
* `Domain` no depende de Infrastructure.
* `Application` no debe depender directamente de PostgreSQL.
* `Api` no accede directamente a EF Core para operaciones de negocio.
* `Controllers` no contienen lógica de negocio.
* `Repositories` no contienen reglas de negocio.
* `Mappers` no contienen reglas de negocio.
* `Validators` centralizan validaciones apropiadas.
* `Services` coordinan los casos de uso.

---

# 5. Convención obligatoria de nombres

Todo el código nuevo utilizará nombres en español.

Ejemplos:

```text
Empresa
Empleado
Conductor
Vehiculo
UnidadOperativa
Servicio
ServicioPasajero
ProgramacionTransporte
Jornada
Usuario
UsuarioRol
Incidencia
Evidencia
Conversacion
Mensaje
Notificacion
```

Métodos:

```text
CrearEmpresaAsync()
ObtenerEmpleadoPorCedulaAsync()
AsignarRolAsync()
PublicarJornadaAsync()
IniciarServicioAsync()
FinalizarServicioAsync()
```

Variables:

```text
empresa
empleado
conductor
servicio
jornada
usuarioActual
```

Interfaces:

```text
IEmpresaService
IEmpleadoService
IConductorService
IUsuarioService
IRepositorioEmpresa
```

No se utilizarán nombres ingleses para las nuevas entidades de negocio salvo nombres estrictamente impuestos por el framework o una librería.

---

# 6. Documentación XML obligatoria

Toda clase, interfaz, enum o componente relevante debe tener documentación XML en español.

Debe explicar:

* qué representa;
* cuál es su responsabilidad;
* qué no debe hacer cuando sea relevante.

Ejemplo conceptual:

```csharp
/// <summary>
/// Representa una empresa cliente de la plataforma.
/// Su responsabilidad es mantener la identidad básica y el estado
/// de activación de la empresa.
/// No debe contener lógica relacionada con la ejecución de servicios.
/// </summary>
public class Empresa
{
}
```

La documentación debe mantenerse durante toda la implementación.

---

# 7. Modelo de identidad y autorización

## 7.1 Usuario

`Usuario` será la identidad global de la persona.

Campos principales:

```text
UsuarioId
Cedula
PasswordHash
Activo
```

La cédula tendrá una restricción única global.

## 7.2 Perfiles

Los perfiles funcionales serán:

```text
Usuario
 ├── Empleado
 └── Conductor
```

Una persona puede tener ambos perfiles.

No se crearán usuarios duplicados para representar diferentes funciones.

## 7.3 Roles

Los roles estarán separados de `Usuario` mediante:

```text
UsuarioRol
```

Esto permite:

```text
Usuario
 ├── EMPLEADO
 ├── CONDUCTOR
 └── COORDINADOR
```

Los roles podrán tener ámbito empresarial.

---

# 8. Administración de roles

## 8.1 Administrador de plataforma

Existe un único concepto de:

```text
ADMINISTRADOR_PLATAFORMA
```

Su función principal es:

* crear empresas;
* configurar inicialmente una empresa;
* asignar el primer coordinador.

Después de la configuración inicial, no participa en la operación cotidiana de la empresa. No gestiona ni conoce la operativa o el personal de las empresas, incluyendo el cambio de empresa de un empleado, que se resuelve automáticamente durante la importación Excel (ver §24).

## 8.2 Coordinador

Un coordinador administra su empresa.

Puede:

* gestionar empleados;
* gestionar conductores;
* gestionar vehículos;
* gestionar sedes;
* gestionar unidades;
* importar Excel;
* gestionar programaciones;
* gestionar jornadas;
* gestionar servicios;
* publicar servicios;
* gestionar la operación;
* asignar el rol `COORDINADOR` a otros usuarios de su propia empresa.

No puede:

* crear empresas;
* administrar otras empresas;
* autoasignarse roles;
* tener el rol `COORDINADOR` activo en más de una empresa simultáneamente;
* ejecutar manualmente un cambio de empresa de un empleado (no existe esa operación manual; se resuelve automáticamente durante la importación Excel).

## 8.3 Asignación de coordinadores

El backend debe comprobar:

```text
Usuario autenticado
        ↓
¿Tiene rol COORDINADOR?
        ↓
¿El rol está activo?
        ↓
¿Pertenece a la empresa?
        ↓
¿El usuario destino pertenece o puede pertenecer a esa empresa?
        ↓
Asignar COORDINADOR
```

La asignación de roles debe estar protegida por autorización del backend.

## 8.4 Revocación de roles

Un coordinador autorizado puede desactivar (`Activo = false`) un `UsuarioRol COORDINADOR` de su propia empresa, siguiendo las mismas validaciones de autorización que la asignación.

Una empresa debe conservar siempre al menos un `COORDINADOR` activo. No se permite revocar el último coordinador activo de una empresa.

La revocación no elimina físicamente el registro `UsuarioRol`.

---

# 9. Selección de rol activo

Un usuario puede poseer varios roles.

Por ejemplo:

```text
Juan
 ├── EMPLEADO
 ├── CONDUCTOR
 └── COORDINADOR
```

El sistema deberá permitir determinar el contexto operativo de la sesión.

La implementación técnica de:

* rol activo;
* claims JWT;
* cambio de rol;
* autorización por endpoint;

deberá definirse durante la implementación de autenticación.

No se debe duplicar la cuenta para cada rol.

---

# 10. Multiempresa

Toda operación empresarial debe ejecutarse dentro de un contexto de empresa válido.

El backend debe determinar la empresa autorizada a partir de:

* usuario autenticado;
* rol;
* vínculo empresarial;
* entidad consultada.

Nunca se confiará únicamente en un `EmpresaId` enviado por React.

Ejemplo:

```text
GET /api/empleados?empresaId=5
```

no significa que el usuario pueda consultar Empresa 5.

El backend debe comprobar que el usuario está autorizado para esa empresa.

---

# 11. Arquitectura de autenticación

Se utilizará:

```text
Cédula + contraseña
        ↓
Autenticación
        ↓
JWT
        ↓
Claims
        ↓
Autorización
```

El JWT deberá permitir identificar:

* `UsuarioId`;
* cédula;
* roles;
* contexto necesario para autorización.

No se almacenarán contraseñas en texto plano.

Las contraseñas se almacenarán mediante hash seguro.

La configuración concreta de expiración, renovación y claims se definirá durante la implementación.

OAuth queda fuera de la primera versión.

---

# 12. Entity Framework Core

Entity Framework Core será la tecnología de persistencia.

Se utilizará:

* `DbContext`;
* configuraciones de entidades;
* relaciones;
* claves primarias;
* claves foráneas;
* índices;
* restricciones únicas;
* migraciones.

Las configuraciones de entidades deberán mantenerse separadas cuando resulte conveniente.

Ejemplo:

```text
Infrastructure/
└── Configurations/
    ├── EmpresaConfiguration.cs
    ├── UsuarioConfiguration.cs
    ├── UsuarioRolConfiguration.cs
    ├── EmpleadoConfiguration.cs
    └── ...
```

No se debe llenar `DbContext` con toda la configuración si separarla mejora la mantenibilidad.

---

# 13. PostgreSQL

PostgreSQL será la base de datos principal.

El modelo debe respetar las relaciones definidas en `data-model.md`.

Se deberán implementar:

* claves primarias;
* claves foráneas;
* índices;
* restricciones únicas;
* restricciones de integridad;
* tipos apropiados;
* relaciones uno a uno;
* relaciones uno a muchos;
* relaciones muchos a muchos mediante entidades explícitas cuando corresponda.

La estrategia concreta para enums de PostgreSQL se decidirá durante la implementación inicial.

---

# 14. Migraciones

Las modificaciones estructurales de base de datos se realizarán mediante migraciones de EF Core.

Flujo:

```text
Modelo C#
    ↓
Migración EF Core
    ↓
PostgreSQL
```

No se deberán modificar manualmente las tablas en producción para cambios que deban estar representados en migraciones.

Cada migración deberá ser revisada antes de aplicarse.

---

# 15. Repositorios

Los repositorios abstraerán la persistencia cuando sea necesario.

Ejemplo:

```text
IEmpresaRepository
IEmpleadoRepository
IConductorRepository
IUsuarioRepository
IServicioRepository
IJornadaRepository
```

Responsabilidad:

* consultar;
* insertar;
* actualizar;
* eliminar cuando esté permitido;
* persistir cambios.

No deben decidir reglas de negocio.

Ejemplo:

```text
Repository
    ↓
"guardar servicio"
```

pero no:

```text
Repository
    ↓
"decidir si el servicio puede publicarse"
```

La segunda responsabilidad pertenece a Application/Rules.

---

# 16. Servicios de aplicación

Cada caso de uso relevante tendrá servicios de aplicación.

Ejemplos:

```text
IEmpresaService
IEmpleadoService
IConductorService
IVehiculoService
IUnidadOperativaService
IUsuarioService
IRolService
ISedeService
IImportacionExcelService
IProgramacionTransporteService
IJornadaService
IServicioService
IServicioPasajeroService
IIncidenciaService
IConversacionService
INotificacionService
```

Los servicios:

* coordinan casos de uso;
* aplican reglas;
* validan permisos;
* coordinan repositorios;
* convierten entidades a DTOs;
* gestionan transacciones cuando sea necesario.

---

# 17. DTOs

Las entidades de dominio no se expondrán directamente como contratos públicos de la API.

Se utilizarán DTOs.

Ejemplos:

```text
CrearEmpresaDto
ActualizarEmpresaDto
EmpresaDto

CrearEmpleadoDto
EmpleadoDto

CrearConductorDto
ConductorDto

CrearServicioDto
ServicioDto

ServicioPasajeroDto
```

Los DTOs deben representar las necesidades de la API.

No deben convertirse en copias automáticas innecesarias de todas las entidades.

---

# 18. Mappers

Los mappers realizarán conversiones entre:

```text
Entity → DTO
DTO → Entity
```

Los mappers no deben:

* consultar base de datos;
* ejecutar reglas de negocio;
* decidir permisos;
* realizar llamadas externas.

---

# 19. Validadores

Las validaciones deberán estar centralizadas cuando sean reutilizables.

Ejemplos:

```text
CedulaValida
TelefonoValido
CapacidadValida
FechaProgramacionValida
ServicioValido
RolValido
```

Debe distinguirse entre:

### Validación de formato

Ejemplo:

```text
¿La cédula tiene formato válido?
```

y:

### Regla de negocio

Ejemplo:

```text
¿Este coordinador puede asignar COORDINADOR a este usuario?
```

La segunda pertenece al caso de uso y autorización.

---

# 20. Empresas y sedes

Implementación inicial:

1. Crear empresa.
2. Activar/desactivar empresa.
3. Crear primer coordinador.
4. Crear sedes.
5. Activar/desactivar sedes.
6. Validar pertenencia empresarial.

El sistema debe impedir crear servicios con sedes de otra empresa.

---

# 21. Empleados

Implementación (operaciones del coordinador sobre su propia empresa):

1. Crear/actualizar empleado.
2. Asociar empleado a Usuario.
3. Gestionar información actual.
4. Consultar empleados de la empresa autorizada.
5. Desactivar empleado.

No existe una operación manual de cambio de empresa. El cambio de empresa de un empleado se detecta y se ejecuta automáticamente durante la importación Excel, cuando la misma cédula (vía `Usuario`) aparece en la importación de una empresa distinta a la actual (ver §24). `ADMINISTRADOR_PLATAFORMA` no participa en este proceso ni en la gestión operativa de empleados.

Al detectarse el cambio, se actualizan en una misma transacción `Empleado.EmpresaId` y el `EmpresaId` del `UsuarioRol` del rol `EMPLEADO` correspondiente. No debe modificarse ningún registro existente de `ProgramacionTransporte`, `ServicioPasajero`, `Jornada` ni `Servicio`; estos conservan la empresa en la que fueron generados.

La cédula identifica globalmente al usuario.

No se deben crear duplicados por cambio de empresa.

---

# 22. Conductores

Implementación:

1. Crear usuario.
2. Crear perfil de conductor.
3. Crear vínculo con empresa.
4. Gestionar múltiples vinculaciones.
5. Activar/desactivar conductor.
6. Gestionar vehículos.
7. Crear unidad operativa.

El conductor puede trabajar para múltiples empresas.

---

# 23. Vehículos y unidades operativas

La creación operacional seguirá:

```text
Conductor
   ↓
Vehículo
   ↓
UnidadOperativa
```

El backend debe comprobar:

```text
UnidadOperativa.ConductorId
==
Vehiculo.ConductorId
```

No se permitirá asociar un vehículo de otro conductor.

## 23.1 Cambio de vehículo

Cuando un conductor deja de utilizar un vehículo, el `Vehiculo` y su `UnidadOperativa` asociada se desactivan (no se eliminan físicamente).

Si el conductor pasa a operar con otro vehículo, se crea una nueva `UnidadOperativa` para la nueva combinación conductor + vehículo. No se reutiliza ni se modifica la unidad anterior.

Los `Servicio` ya creados conservan la `UnidadOperativa` que efectivamente utilizaron; no se modifican retroactivamente. Los servicios nuevos utilizan la `UnidadOperativa` activa vigente al momento de asignarse. (La `UnidadOperativa` pertenece a `Servicio`, no a `Jornada`; ver §26-27.)

---

# 24. Importación Excel

La importación será implementada como un caso de uso independiente.

Flujo:

```text
Archivo Excel
    ↓
Validación
    ↓
Lectura
    ↓
Normalización
    ↓
Identificación por cédula
    ↓
Crear/actualizar Usuario
    ↓
Crear/actualizar Empleado
    ↓
Crear ProgramacionTransporte
    ↓
Resultado de importación
```

Reglas:

* no borrar automáticamente programaciones anteriores;
* permitir múltiples archivos;
* detectar registros nuevos;
* detectar registros idénticos;
* detectar diferencias;
* informar conflictos;
* si la cédula ya existe asociada a otra empresa, actualizar automáticamente y en una misma transacción `Empleado.EmpresaId` y el `EmpresaId` del `UsuarioRol` del rol `EMPLEADO`, sin modificar registros históricos;
* conservar histórico.

El campo `Estado` de `ImportacionExcel` utiliza los valores definidos en `data-model.md` §13.3: `PENDIENTE`, `PROCESANDO`, `COMPLETADA`, `COMPLETADA_CON_ADVERTENCIAS`, `ERROR`.

El formato exacto del Excel permanece pendiente hasta disponer del archivo real.

No se debe inventar el formato.

---

# 25. Programaciones

Implementación:

```text
Empleado
   ↓
ProgramacionTransporte
   ↓
ServicioPasajero
   ↓
Servicio
```

Una programación puede existir inicialmente sin servicio pasajero asignado.

Debe conservar:

* empresa;
* empleado;
* sede;
* fecha;
* hora;
* tipo;
* dirección de recogida;
* barrio.

---

# 26. Jornadas

Una jornada agrupa los servicios de una empresa para una fecha operativa determinada. No agrupa servicios de una única unidad operativa: una misma jornada puede involucrar múltiples unidades distintas, cada una asignada individualmente a su propio servicio.

```text
Empresa
    ↓
  Jornada
    ↓
 Servicios ── cada uno con su propia UnidadOperativa (opcional hasta asignarse)
```

La jornada:

* tiene empresa;
* tiene fecha operativa;
* no tiene unidad operativa propia;
* no tiene hora de inicio;
* no tiene hora de finalización.

Los servicios pueden pertenecer a fechas calendario diferentes de `FechaOperativa`.

---

# 27. Servicios

Los servicios tendrán:

```text
JornadaId
UnidadOperativaId (nulo hasta que se asigne)
SedeId
Fecha
HoraProgramada
Tipo
Estado
HoraInicioReal
HoraFinReal
```

No se utilizará un único `DateTime` para reemplazar la separación de fecha y hora aprobada.

La empresa se obtiene mediante:

```text
Servicio
 ↓
Jornada
 ↓
Empresa
```

La unidad operativa pertenece directamente al servicio:

```text
Servicio.UnidadOperativaId
```

y no se obtiene a través de la jornada. Dos servicios de la misma jornada pueden tener unidades operativas distintas. Reasignar la unidad operativa de un servicio modifica únicamente `Servicio.UnidadOperativaId`; `Servicio.JornadaId` permanece igual.

---

# 28. Estados de servicio

Se implementarán:

```text
BORRADOR
PENDIENTE_ASIGNACION
ASIGNADO
PUBLICADO
EN_CURSO
FINALIZADO
CANCELADO
```

Las transiciones válidas deben centralizarse.

No se permitirá que cualquier endpoint modifique directamente el estado sin aplicar las reglas correspondientes.

No se implementará historial automático de estados en esta versión.

---

# 29. Servicios pasajeros

Se implementarán los estados definidos en `data-model.md`.

La aplicación debe soportar:

* confirmación;
* no asistencia;
* llegada;
* espera;
* incidencias;
* recogida;
* viaje;
* llegada a destino.

El orden de pasajeros podrá modificarse por el conductor.

La dirección y coordenadas del servicio pasajero representan el contexto histórico concreto de ese servicio.

---

# 30. Planificación de rutas

La planificación se implementará inicialmente como un módulo de aplicación independiente.

Debe recibir información como:

* servicios;
* horarios;
* pasajeros;
* capacidades;
* sedes;
* ubicaciones;
* jornadas;
* unidad operativa;
* servicios anteriores y posteriores.

Debe producir:

```text
Propuesta de asignación
Propuesta de orden
Recomendación de planificación
```

El algoritmo no puede sustituir la decisión del coordinador.

No debe modificar automáticamente la planificación sin autorización explícita.

---

# 31. Reglas de planificación

La propuesta deberá considerar:

* capacidad;
* horarios;
* ubicación;
* continuidad geográfica;
* tiempos estimados;
* llegada mínima de 15 minutos antes de la entrada;
* un tiempo conjunto aproximado de recogida (referencia inicial: 25 minutos; valor parametrizable y no definitivo);
* ubicación del servicio anterior;
* ubicación del siguiente servicio;
* carga de trabajo.

Para entradas se utilizará como heurística:

```text
Más lejano
    ↓
Más cercano
    ↓
Sede
```

El valor de 25 minutos de la ventana de recogida es una referencia inicial de implementación, no una regla de negocio cerrada. Debe implementarse como parámetro configurable.

La fórmula exacta y pesos se definirán cuando se implemente el algoritmo.

---

# 32. Publicación

El caso de uso de publicación comprobará:

```text
Jornada
 ↓
Servicios
 ↓
Asignaciones
 ↓
Consistencia
 ↓
¿Todo resuelto?
 ↓
PUBLICAR
```

No se podrá publicar una jornada con servicios sin resolver.

Una jornada publicada podrá ser modificada según las reglas de estado.

Los usuarios afectados deberán recibir notificaciones cuando corresponda.

---

# 33. Ejecución del servicio

El conductor podrá:

```text
Ver servicio
    ↓
Iniciar ruta
    ↓
Navegar
    ↓
Llegar pasajero
    ↓
Esperar
    ↓
Procesar pasajero
    ↓
Continuar
    ↓
Finalizar
```

Se almacenará:

```text
HoraInicioReal
HoraFinReal
```

Las coordenadas de ejecución podrán incorporarse donde el modelo las contemple.

---

# 34. Geolocalización

La geolocalización se utilizará para:

* detectar llegada;
* facilitar navegación;
* registrar ubicación en incidencias;
* permitir futuras reglas de geocerca.

Debe existir alternativa manual cuando la detección automática no sea confiable.

El proveedor y radio exacto de geocerca quedan pendientes.

No se debe introducir una distancia arbitraria sin decisión previa.

---

# 35. Incidencias y evidencias

Las incidencias serán entidades independientes asociadas a `ServicioPasajero`.

Flujo:

```text
ServicioPasajero
      ↓
  Incidencia
      ↓
   Evidencia
```

Una incidencia puede tener múltiples evidencias.

Las evidencias almacenarán referencias externas, no archivos binarios directamente en PostgreSQL.

El único tipo de evidencia contemplado inicialmente es `FOTOGRAFIA` (ver `data-model.md` §20.2).

El almacenamiento concreto se decidirá durante la implementación.

---

# 36. Chat

El chat será individual:

```text
ServicioPasajero
      ↓
Conversacion
      ↓
Mensajes
```

La conversación será privada entre conductor y empleado.

No se implementará chat grupal.

Las llamadas se realizarán mediante las capacidades del dispositivo y no se grabarán.

---

# 37. Notificaciones

Se implementará almacenamiento de notificaciones para:

* cambios de programación;
* publicación;
* incidencias relevantes;
* recordatorios;
* modificaciones que afecten directamente a usuarios.

Las notificaciones deben dirigirse solamente a usuarios afectados cuando la regla lo establezca.

---

# 38. Alerta de ruta no iniciada

Debe existir un mecanismo que permita detectar:

```text
Servicio de entrada
      ↓
Una hora antes
      ↓
¿Ruta iniciada?
      ↓
NO
      ↓
Notificar conductor responsable
```

La alerta es para el conductor responsable.

No constituye una orden de inicio.

La lógica debe considerar posteriormente la recomendación de hora de inicio calculada por el algoritmo.

**Actualización (2026-09-30, decisión del usuario):** la alerta también llega a los demás conductores de la misma empresa que tienen ruta publicada o en curso a la misma fecha y hora, para que se comuniquen entre ellos (con el nombre y teléfono del conductor que no ha iniciado). Los textos describen la ruta de forma legible —nombre del conductor, tipo, hora en formato de 12 horas, sede y fecha—, nunca solo códigos internos (por ejemplo: "Andres Felipe Otalvaro no ha iniciado la ruta de entrada de la 01:00 a. m. a La Patria (30/09/2026)"). El aviso se repite en cada verificación (cada 15 minutos) mientras la ruta siga sin iniciarse, desde una hora antes y hasta la hora programada (a la hora de entrada ese horario ya quedó atrás y no se sigue avisando), indicando cuánto falta; solo se evita repetirlo en menos de 10 minutos. Sigue sin notificarse al coordinador.

---

# 39. API

La API utilizará endpoints REST.

Los controllers deben:

* recibir HTTP;
* validar entrada básica;
* obtener usuario autenticado;
* llamar al servicio correspondiente;
* devolver códigos HTTP apropiados.

No deben:

* consultar directamente `DbContext`;
* contener lógica de negocio compleja;
* modificar entidades directamente.

Ejemplo:

```text
POST /api/servicios
GET /api/servicios/{id}
POST /api/servicios/{id}/iniciar
POST /api/servicios/{id}/finalizar
POST /api/jornadas/{id}/publicar
```

Los nombres concretos de endpoints se definirán en los contratos OpenAPI.

---

# 40. Swagger / OpenAPI

La API deberá documentarse mediante Swagger/OpenAPI.

Los contratos deben definir:

* endpoints;
* parámetros;
* DTOs;
* respuestas;
* errores;
* autenticación;
* autorización.

El contrato debe mantenerse sincronizado con la implementación.

---

# 41. Manejo de errores

Se implementará una estrategia centralizada de errores HTTP.

Debe evitarse devolver excepciones internas directamente al cliente.

La API debe proporcionar respuestas consistentes para:

* validaciones;
* autenticación;
* autorización;
* recursos inexistentes;
* conflictos;
* errores internos.

Se podrá utilizar middleware global para centralizar el tratamiento.

---

# 42. Transacciones

Los casos de uso que modifiquen varias entidades relacionadas deberán considerar transacciones.

Ejemplo:

```text
Crear conductor
    ↓
Crear Usuario
    ↓
Crear Conductor
    ↓
Crear Vinculación
    ↓
Crear Vehículo
    ↓
Crear UnidadOperativa
```

Si una operación requiere consistencia entre varios pasos, deberá confirmarse o revertirse de forma atómica.

No se deben introducir transacciones innecesarias en operaciones simples.

---

# 43. Pruebas unitarias

Se utilizará xUnit.

Las pruebas unitarias deberán cubrir principalmente:

* reglas de negocio;
* autorización;
* roles;
* multiempresa;
* transiciones de estados;
* validaciones;
* planificación;
* asignaciones;
* cambios de empresa;
* lógica de importación.

Las reglas importantes no deben depender exclusivamente de pruebas de integración.

---

# 44. Pruebas de integración

Las pruebas de integración verificarán:

```text
API
 ↓
Application
 ↓
Infrastructure
 ↓
PostgreSQL
```

Se probarán especialmente:

* autenticación;
* autorización;
* aislamiento multiempresa;
* relaciones;
* restricciones de base de datos;
* creación de jornadas;
* servicios;
* publicación;
* ejecución;
* importación.

La estrategia exacta de base de datos de pruebas se definirá durante la implementación.

---

# 45. Docker

La aplicación debe poder ejecutarse mediante Docker.

Se contemplará inicialmente:

```text
docker-compose
   ├── API
   └── PostgreSQL
```

El objetivo es disponer de un entorno reproducible para desarrollo e integración.

Las credenciales y secretos no deben quedar hardcodeados.

---

# 46. Configuración

Las configuraciones sensibles deberán utilizar:

* variables de entorno;
* secretos de desarrollo;
* configuración segura del entorno.

No se deben almacenar en Git:

* contraseñas;
* claves JWT;
* cadenas de conexión reales;
* tokens;
* credenciales externas.

Se incluirán archivos de ejemplo cuando sea necesario.

---

# 47. Índices iniciales

Se evaluarán índices para:

* `Usuario.Cedula`;
* `UsuarioRol.UsuarioId`;
* `UsuarioRol.EmpresaId`;
* `Empleado.UsuarioId`;
* `Empleado.EmpresaId`;
* `Conductor.UsuarioId`;
* `Vehiculo.Placa`;
* `ProgramacionTransporte.Fecha`;
* `ProgramacionTransporte.EmpleadoId`;
* `Servicio.Fecha`;
* `Servicio.Estado`;
* `ServicioPasajero.ServicioId`;
* `Notificacion.UsuarioId`;
* `Mensaje.ConversacionId`.

`Empleado` no almacena `Cedula`. La identidad de la persona se obtiene siempre mediante:

```text
Empleado → Usuario → Cedula
```

No se creará ningún índice `Empleado.Cedula`; el índice de cédula se mantiene únicamente en `Usuario.Cedula` (ya listado arriba).

Los índices definitivos deben basarse en las consultas reales y no agregarse indiscriminadamente.

---

# 48. Zona horaria

**Decisión anterior (2026-09-18), sustituida por la actualización del 2026-09-30 que aparece más abajo:** toda la plataforma opera en UTC. `Fecha`, `HoraProgramada`, `FechaOperativa`, `HoraInicioReal`, `HoraFinReal` y cualquier otro campo de fecha/hora se interpretan, se almacenan y se comparan en UTC (`DateTime.UtcNow` en el backend), sin conversión a hora local en ninguna capa del servidor. La conversión a hora local para mostrarla al usuario es responsabilidad exclusiva del frontend.

Se diferencia:

* fecha calendario (`Servicio.Fecha`);
* hora programada (`Servicio.HoraProgramada`, `ProgramacionTransporte.Hora`);
* timestamp de ejecución (`HoraInicioReal`, `HoraFinReal`, `FechaHora` de notificaciones/mensajes/incidencias — todos en UTC);
* fecha operativa (`Jornada.FechaOperativa`).

**Actualización (2026-09-30, decisión del usuario):** la aplicación es para Colombia. Las fechas y horas **programadas** (`Servicio.Fecha`, `Servicio.HoraProgramada`, `Jornada.FechaOperativa`, `ProgramacionTransporte.Fecha`/`Hora`) se guardan y se interpretan en **hora de Colombia (UTC−5 fijo)**, tal como las escribe el coordinador; cualquier comparación con "ahora" usa `ReglasHoraColombia.AhoraColombia(DateTime.UtcNow)`. Los **timestamps de ejecución** siguen en UTC y el frontend los muestra en hora de Colombia. Ver `AGENTS.md` §41.

---

# 49. Orden de implementación

La implementación se dividirá en fases.

## Fase 1 — Fundación

* crear solución;
* crear proyectos;
* configurar referencias;
* configurar convenciones;
* configurar PostgreSQL;
* configurar EF Core;
* configurar Docker;
* configurar Swagger;
* configurar estructura de pruebas.

## Fase 2 — Identidad

* Usuario;
* UsuarioRol;
* autenticación;
* JWT;
* autorización;
* activación de cuenta;
* roles;
* permisos.

## Fase 3 — Empresas

* Empresa;
* Sede;
* administración inicial;
* primer coordinador;
* coordinadores adicionales;
* aislamiento empresarial.

## Fase 4 — Personas

* Empleado;
* Conductor;
* relaciones Usuario/Empleado;
* relaciones Usuario/Conductor;
* cambio de empresa;
* vinculaciones conductor-empresa.

## Fase 5 — Vehículos

* Vehiculo;
* UnidadOperativa;
* validaciones conductor/vehículo.

## Fase 6 — Importación y programación

* ImportacionExcel;
* lectura y validación;
* empleados;
* programaciones;
* detección de duplicados/conflictos.

El formato real del Excel se implementará únicamente después de definirlo.

## Fase 7 — Jornadas y servicios

* Jornada;
* Servicio;
* estados;
* asignaciones;
* ServicioPasajero;
* orden de pasajeros.

## Fase 8 — Planificación

* reglas;
* heurísticas;
* propuesta de rutas;
* propuesta de asignaciones;
* validaciones de capacidad y horario;
* integración con jornadas.

## Fase 9 — Publicación

* publicación;
* notificaciones;
* modificaciones;
* afectados.

## Fase 10 — Ejecución

* inicio;
* llegada;
* espera;
* estados;
* navegación;
* finalización;
* geolocalización.

## Fase 11 — Incidencias y evidencias

* incidencias;
* evidencias;
* almacenamiento externo.

## Fase 12 — Chat

* conversaciones;
* mensajes;
* autorización conductor/empleado.

## Fase 13 — Alertas

* ruta no iniciada;
* recordatorios;
* notificaciones operativas.

## Fase 14 — Endurecimiento

* pruebas;
* seguridad;
* índices;
* errores;
* validaciones;
* documentación;
* migraciones;
* Docker;
* revisión de regresiones.

---

# 50. Estrategia de commits

Los cambios deben realizarse en unidades pequeñas y coherentes.

Ejemplos:

```text
feat: crear estructura inicial de la solucion
feat: implementar identidad y usuarios
feat: implementar roles y autorizacion
feat: implementar empresas y sedes
feat: implementar empleados
feat: implementar conductores
feat: implementar vehiculos y unidades operativas
feat: implementar programaciones
feat: implementar jornadas y servicios
feat: implementar planificacion
```

No se deben mezclar cambios funcionales no relacionados en un mismo commit.

---

# 51. Definition of Done

Una tarea funcional no puede marcarse como completada únicamente porque el código compila.

Una tarea se considera terminada cuando:

* el código compila;
* las reglas correspondientes están implementadas;
* existen pruebas cuando corresponde;
* las pruebas pasan;
* no se rompe funcionalidad existente;
* las migraciones funcionan;
* la documentación relevante está actualizada;
* los nombres respetan las convenciones;
* las clases tienen documentación XML;
* no existen secretos en el repositorio;
* se respeta la separación de capas;
* la tarea puede relacionarse claramente con un requisito de `spec.md`.

---

# 52. Reglas para Claude Code

Claude Code debe actuar como implementador.

Antes de modificar arquitectura o comportamiento debe revisar:

```text
CONSTITUTION.md
AGENTS.md
spec.md
data-model.md
plan.md
tasks.md
```

Debe:

* implementar los requisitos aprobados;
* mantener trazabilidad;
* respetar las capas;
* crear pruebas;
* informar problemas;
* no inventar requisitos;
* no cambiar reglas de negocio unilateralmente;
* no introducir tecnologías innecesarias;
* no crear microservicios;
* no crear patrones innecesarios;
* no duplicar entidades;
* no duplicar usuarios;
* no duplicar lógica.

Si encuentra una contradicción entre documentos, debe detener la implementación de esa parte y reportar la contradicción antes de tomar una decisión de negocio.

---

# 53. Principio de no sobreingeniería

La solución debe mantenerse sencilla.

No introducir inicialmente:

* CQRS;
* Event Sourcing;
* microservicios;
* MediatR si no aporta una necesidad real;
* buses de eventos;
* sistemas de mensajería;
* repositorios genéricos innecesarios;
* patrones abstractos sin beneficio;
* IA para decisiones que puede resolver una regla simple.

La complejidad debe introducirse solamente cuando exista una necesidad demostrable.

---

# 54. Trazabilidad

Cada implementación deberá poder relacionarse con:

```text
Requisito
   ↓
Caso de uso
   ↓
Servicio
   ↓
Entidad
   ↓
Endpoint
   ↓
Prueba
```

La trazabilidad debe mantenerse especialmente en funcionalidades críticas como:

* autenticación;
* autorización;
* multiempresa;
* roles;
* planificación;
* publicación;
* ejecución de servicios.

---

# 55. Decisiones pendientes

Las siguientes decisiones permanecen abiertas y no deben ser inventadas:

1. Formato exacto del Excel.
2. Proveedor de geocodificación.
3. Proveedor de mapas.
4. Radio de geocerca.
5. Fórmula exacta del algoritmo de rutas.
6. Pesos de los criterios de planificación.
7. Estrategia de almacenamiento de evidencias.
8. Política exacta de renovación JWT.
9. Valores definitivos de algunos enums.
10. Reglas exactas de edición después de publicar.
11. Índices definitivos basados en consultas reales.
12. Auditoría de cambios de roles.
13. Auditoría de acciones administrativas.

La validación de identidad durante la activación se resuelve mediante un enlace enviado por correo (revisado el 2026-09-19; antes: código de activación generado por el conductor, ya eliminado). Ver AGENTS.md §16 (ver §28 de `data-model.md` y §10 de `spec.md`).

La estrategia de zona horaria ya no es una decisión pendiente (decisión vigente, 2026-09-30): la aplicación opera en hora de Colombia (UTC−5 fijo): las fechas y horas programadas (`Fecha`, `HoraProgramada`, `FechaOperativa`) se guardan y comparan en hora de Colombia, y los instantes reales (`HoraInicioReal`, `HoraFinReal`, `FechaHora`) se guardan en UTC y el frontend los muestra en hora de Colombia (ver `AGENTS.md` §41 y `spec.md` §47).

La estrategia exacta de roles activos en JWT ya no es una decisión pendiente (decisión cerrada, 2026-09-18): ver `spec.md` §47 y `GeneradorTokenJwt`.

La estrategia definitiva de pruebas de integración con PostgreSQL ya no es una decisión pendiente (decisión cerrada, 2026-09-18): ver `spec.md` §47 y `tasks.md` T114-T118.

Estas decisiones deberán resolverse antes de implementar las partes que dependan directamente de ellas.

---

# 56. Resultado esperado

Al finalizar la primera versión se debe disponer de una API .NET modular y mantenible que permita:

```text
ADMINISTRADOR_PLATAFORMA
        │
        ├── Crear empresa
        └── Crear primer coordinador
                    │
                    ▼
              COORDINADOR
                    │
        ┌───────────┼───────────┐
        ▼           ▼           ▼
    Empleados   Conductores   Sedes
                    │
                 Vehículos
                    │
              Unidades Operativas
                    │
              Programaciones
                    │
                 Jornadas
                    │
                 Servicios
                    │
              ServicioPasajero
                    │
        ┌───────────┼───────────┐
        ▼           ▼           ▼
    Ejecución   Incidencias    Chat
                    │
                 Evidencias
```

La plataforma debe mantener una identidad única por persona, permitir múltiples roles, mantener aislamiento empresarial y permitir que cada empresa sea administrada autónomamente por sus coordinadores después de la configuración inicial.

La prioridad será construir primero una base sólida y sencilla antes de incorporar funcionalidades avanzadas.

---

# 57. Frontend (React)

## 57.1 Propósito

`spec.md` §42 y `CONSTITUTION.md` ya establecen que existirá un frontend (mencionado como "React" en varios ejemplos de este mismo documento y de `CONSTITUTION.md`/`tasks.md`), responsable de presentación, navegación, formularios y visualización, nunca de seguridad. Esta sección define la arquitectura técnica concreta para implementarlo, ya que no estaba especificada.

## 57.2 Tecnologías

* React.
* Vite (herramienta de compilación y servidor de desarrollo).
* TypeScript.
* Cliente HTTP para consumir la API REST de `LFMova.Api`.

No se introducen frameworks de gestión de estado, UI kits ni librerías adicionales sin una necesidad concreta (coherente con `AGENTS.md` §34, no sobreingeniería).

## 57.3 Ubicación e independencia de la solución .NET

El frontend vive en una carpeta independiente `frontend/` en la raíz del repositorio, **fuera** de `LFMova.sln`. No es un proyecto .NET y no se agrega a la solución. Tiene su propio `package.json`, control de dependencias y ciclo de vida de build, separado del backend.

```text
LFMova/
├── src/            (backend .NET, dentro de LFMova.sln)
├── tests/          (backend .NET)
├── docs/
└── frontend/       (React + Vite + TypeScript, independiente del .sln)
```

## 57.4 Estructura interna del frontend

```text
frontend/
├── src/
│   ├── componentes/    (componentes de UI reutilizables)
│   ├── paginas/        (pantallas/rutas de la aplicación)
│   ├── servicios/      (clientes de la API REST, uno por recurso del backend)
│   ├── rutas/           (definición de enrutamiento y rutas protegidas)
│   ├── modelos/         (tipos/DTOs TypeScript que reflejan los DTOs de Application)
│   └── contexto/        (contexto de autenticación: token JWT, usuario actual)
├── index.html
├── package.json
├── tsconfig.json
└── vite.config.ts
```

Nombres de carpetas, componentes, funciones y variables en español, salvo términos técnicos obligatorios del framework/ecosistema (`props`, `hooks`, `useState`, nombres de paquetes npm, etc.), igual que en el backend (`AGENTS.md` §2).

## 57.5 Consumo de la API y autenticación JWT

* La URL base de la API se configura mediante variables de entorno de Vite (`import.meta.env`), nunca hardcodeada en el código fuente.
* El token JWT obtenido en `POST /api/autenticacion/iniciar-sesion` se conserva en el contexto de autenticación del frontend (en memoria de la aplicación) y se adjunta como cabecera `Authorization: Bearer` en cada solicitud a la API. El almacenamiento persistente (p. ej. `localStorage`) y la renovación de sesión quedan como detalle de implementación a refinar; no se ha definido una política de expiración/renovación distinta a la ya pendiente en `spec.md` §47.
* Las rutas del frontend que requieren sesión se protegen client-side (redirigen a la pantalla de inicio de sesión si no hay token), pero esto es únicamente experiencia de usuario: la autorización real siempre se valida en el backend (`CONSTITUTION.md` §14, `AGENTS.md` §13), nunca solo en el cliente.

## 57.6 Alcance funcional inicial

El frontend no debe implementar pantallas ni flujos que no correspondan a funcionalidad ya implementada en el backend. Cada pantalla nueva se agrega cuando el endpoint que consume ya existe y está probado. No se inventan pantallas especulativas.

## 57.7 Incorporación en el plan de ejecución

El desarrollo del frontend no bloquea ni sustituye al desarrollo del backend: avanza de forma incremental, en paralelo, agregando pantallas a medida que existen endpoints funcionales que consumir (ver `tasks.md`, Fase 7B en adelante). No se espera a que el backend esté terminado para comenzar ni para mostrar avances.
