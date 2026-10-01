Antes de realizar cualquier implementación, leer:

docs/sdd/00-governance/CONSTITUTION.md
docs/sdd/01-specification/spec.md
docs/sdd/02-data-model/data-model.md
docs/sdd/03-architecture/plan.md
docs/sdd/04-execution/tasks.md
docs/sdd/05-validation/checklist.md


# AGENTS.md

## 1. Rol de Claude

Claude Code es el implementador técnico del proyecto.

Debe implementar las especificaciones, reglas de negocio y arquitectura previamente definidas.

No debe actuar como diseñador autónomo de nuevas reglas de negocio.

Cuando una decisión ya está definida en:

* `CONSTITUTION.md`
* `spec.md`
* `data-model.md`
* `plan.md`

debe respetarla.

Si existe contradicción entre documentos, debe señalarla antes de implementar la parte afectada.

---

# 2. Idioma obligatorio

Todo el proyecto debe utilizar español para nombres propios del código.

Ejemplos obligatorios:

```text
Empleado
Conductor
Vehiculo
Empresa
Sede
Jornada
Servicio
ServicioPasajero
ProgramacionTransporte
UnidadOperativa
Incidencia
Evidencia
Conversacion
Mensaje
Notificacion
Usuario
```

Interfaces:

```text
IEmpleadoServicio
IConductorServicio
IEmpleadoRepositorio
```

Implementaciones:

```text
EmpleadoServicio
ConductorServicio
EmpleadoRepositorio
```

DTOs:

```text
EmpleadoDto
CrearEmpleadoDto
ActualizarEmpleadoDto
```

Métodos:

```text
CrearEmpleadoAsync()
ActualizarEmpleadoAsync()
ObtenerPorCedulaAsync()
ValidarAccesoEmpresaAsync()
```

Variables y parámetros también deben utilizar nombres en español.

---

# 3. Documentación XML

Cada clase, interfaz, enum y componente importante debe tener documentación XML en español.

La documentación debe indicar:

* Qué representa.
* Responsabilidad principal.
* Qué no debe hacer.

No utilizar comentarios genéricos que no aporten información.

---

# 4. Estructura

Respetar la separación:

```text
Api
Application
Domain
Infrastructure
```

y:

```text
Controllers
DTOs
Interfaces
Services
Implementations
Mappers
Validators
Utils
Entities
Enums
Rules
Data
Repositories
```

No mover lógica entre capas únicamente para simplificar temporalmente una implementación.

---

# 5. Controllers

Los controladores:

* Reciben solicitudes HTTP.
* Validan aspectos básicos de entrada.
* Obtienen el usuario autenticado.
* Invocan servicios.
* Devuelven respuestas HTTP.

No deben contener:

* Consultas EF Core.
* Reglas complejas.
* Algoritmos de planificación.
* Lógica de negocio.
* Construcción manual de múltiples entidades.

---

# 6. Services

Los servicios contienen la lógica de aplicación y coordinación de operaciones.

Deben utilizar interfaces de repositorio cuando necesiten persistencia.

No deben acceder directamente a `DbContext` salvo que una decisión arquitectónica documentada lo requiera.

---

# 7. Repositories

Los repositorios son responsables de persistencia y consultas.

No deben decidir reglas de negocio.

Ejemplo incorrecto:

```text
Repositorio decide si un coordinador puede modificar un empleado.
```

Eso corresponde a la capa de aplicación/autorización.

---

# 8. Domain

Las entidades representan el modelo del negocio.

No deben depender de:

```text
ASP.NET Core
Controllers
HTTP
Swagger
PostgreSQL
```

Las reglas específicas del dominio deben mantenerse separadas cuando su complejidad lo justifique.

---

# 9. DTOs

Los DTOs representan contratos de entrada y salida de la API.

No utilizar entidades de Entity Framework como contratos públicos por comodidad.

Separar:

```text
CrearEmpleadoDto
ActualizarEmpleadoDto
EmpleadoDto
```

cuando los requisitos sean diferentes.

---

# 10. Mappers

Los mappers son responsables de transformar:

```text
Entity → DTO
DTO → Entity
```

cuando corresponda.

No deben contener reglas de negocio.

---

# 11. Validadores

Las validaciones de formato y reglas reutilizables deben centralizarse.

Ejemplos:

```text
CedulaValidador
EmpleadoValidador
ServicioValidador
```

No duplicar la misma validación en múltiples controladores.

---

# 12. Multiempresa

Toda consulta o modificación que afecte información empresarial debe verificar el contexto de empresa correspondiente.

Nunca asumir que un ID recibido por API pertenece a la empresa del usuario.

Ejemplo:

```text
GET /empleados/25
```

no implica automáticamente que el empleado 25 sea accesible.

Debe verificarse la relación correspondiente.

---

# 13. Seguridad por rol

Las autorizaciones deben comprobarse en backend.

Roles:

```text
ADMINISTRADOR_PLATAFORMA
COORDINADOR
CONDUCTOR
EMPLEADO
```

No implementar autorización basada exclusivamente en el frontend.

---

# 14. Conductor multiempresa

Un conductor puede trabajar para varias empresas.

No utilizar `Usuario.EmpresaId` para representar la empresa del conductor.

La relación debe resolverse mediante:

```text
Usuario
 ↓
Conductor
 ↓
VinculacionConductorEmpresa
 ↓
Empresa
```

---

# 15. Empleado y cambio de empresa

Debe existir un único `Empleado` por cédula.

No existe una operación manual de transferencia de un empleado entre empresas. El cambio de empresa se detecta y se ejecuta automáticamente durante la importación Excel, al encontrar la misma `Cedula` (vía `Usuario`) en la importación de una empresa distinta a la actual. `ADMINISTRADOR_PLATAFORMA` no participa en este proceso ni en la gestión operativa de empleados; un `COORDINADOR` solo gestiona empleados de su propia empresa (ver `spec.md` §39).

Al detectarse el cambio, deben actualizarse en la misma transacción `Empleado.EmpresaId` y `UsuarioRol` del rol `EMPLEADO` (su `EmpresaId`).

Si un empleado cambia de empresa:

```text
Empleado.EmpresaId
```

se actualiza.

No crear otro empleado.

No modificar registros históricos.

No modificar servicios históricos.

No modificar programaciones históricas.

---

# 16. Usuario

`PasswordHash` puede ser `NULL`.

Interpretación:

```text
NULL → cuenta pendiente de activación
valor → cuenta activada
```

No crear un campo `EstadoActivacion` en `Usuario`: el estado de activación sigue derivándose únicamente de `PasswordHash`.

**Decisión vigente (2026-09-19, sustituye a la anterior):** las cuentas se crean por **autorregistro abierto** (correo, nombre completo, cédula, teléfono y contraseña) y la identidad se confirma por **enlace enviado al correo** (`TokenVerificacion`, proveedor Brevo). Una cuenta nueva recibe el rol `EMPLEADO` sin empresa (`UsuarioRol.EmpresaId = NULL`) y no puede iniciar sesión hasta confirmar su correo. Solo hay dos casos de cuenta creada por invitación, ambos sin contraseña escrita por terceros: el primer coordinador que crea el administrador de plataforma y los coordinadores adicionales; reciben un correo para establecer su propia contraseña. Un conductor es una persona ya registrada a la que un coordinador busca por cédula y a la que asigna el rol `CONDUCTOR`; nunca se crea una cuenta desde ese flujo. El inicio de sesión acepta cédula o correo. Existe recuperación de contraseña por correo. La entidad `CodigoActivacion` y su flujo (código generado por un conductor) se eliminaron por completo. No inventar mecanismos adicionales (WhatsApp, SMS, códigos entregados por terceros).

**Decisión vigente (2026-09-29): invitación por correo a una empresa (`InvitacionEmpresa`).** Por privacidad, un `COORDINADOR` solo puede ver los datos de personas que ya forman parte de su empresa (empleados de la empresa, conductores vinculados a ella, sus coordinadores o quienes aceptaron una invitación suya) y solo a ellas puede asignarles los roles `CONDUCTOR` o `COORDINADOR`; la búsqueda por cédula de cualquier otra persona responde igual que si no existiera. Para sumar a alguien ajeno a la empresa, el coordinador la invita por cédula y correo: si la cédula ya tiene cuenta con correo, la invitación va siempre al correo de esa cuenta (nunca al escrito por el coordinador) y el coordinador no ve si existía ni a qué correo se envió. La invitación vence a los 7 días y una nueva reemplaza a la pendiente anterior. La persona la acepta desde el enlace: con sesión iniciada si ya tiene cuenta, o registrándose desde el enlace (queda unida a la empresa y, si usa el mismo correo de la invitación, con el correo ya confirmado). Al aceptar queda como `Empleado` de la empresa sin dirección ni barrio (no es pasajera: va a ser coordinador o conductor) y su `UsuarioRol` `EMPLEADO` toma esa empresa; si ya era empleado de otra empresa no se la mueve (ver §15), pero igual queda relacionada con la empresa por la invitación aceptada. El administrador de plataforma no tiene esta restricción. Ver `InvitacionEmpresaServicio` y `PersonaServicio`.

---

# 17. Histórico

Nunca actualizar información histórica únicamente porque cambió la información actual de una entidad.

Ejemplo:

```text
Empleado.Direccion
```

puede cambiar.

Pero:

```text
ServicioPasajero.DireccionRecogida
```

debe conservar la ubicación utilizada para ese servicio.

---

# 18. Estados

Mantener solamente el estado actual salvo especificación explícita de historial.

No crear automáticamente:

```text
HistorialEstadoServicio
HistorialEstadoServicioPasajero
```

porque no forman parte del diseño actual.

---

# 19. Servicio y Jornada

**Decisión cerrada (corrige una interpretación anterior de este mismo documento):** `Servicio` sí tiene `UnidadOperativaId` propio (opcional/nulo hasta que se asigne). `Jornada` NO tiene `UnidadOperativaId`.

La relación correcta es:

```text
Empresa
    ↓
Jornada
    ↓
Servicio ── UnidadOperativaId (asignación individual por servicio)
```

La unidad del servicio se obtiene directamente de `Servicio.UnidadOperativaId`, no a través de la jornada. Una misma `Jornada` puede tener servicios asignados a distintas `UnidadOperativa`. Reasignar la unidad de un servicio modifica únicamente `Servicio.UnidadOperativaId`, nunca `Servicio.JornadaId`. Ver `data-model.md` §15-16 y `plan.md` §26-27.

---

# 20. Fechas y horas

Mantener separadas las propiedades cuando así se definieron:

```text
Jornada.FechaOperativa
Servicio.Fecha
Servicio.HoraProgramada
ProgramacionTransporte.Fecha
ProgramacionTransporte.Hora
```

No reemplazarlas automáticamente por un único `DateTime`.

---

# 21. Jornada

`Jornada` es el bloque operativo de una empresa para una fecha operativa determinada. No es una agrupación de servicios de una única unidad: puede involucrar múltiples `UnidadOperativa` distintas a través de sus servicios (ver §19).

No tiene:

```text
UnidadOperativaId
HoraInicio
HoraFin
```

`FechaOperativa` representa la fecha de referencia operacional, no necesariamente la fecha calendario del primer servicio.

Una jornada puede contener servicios que crucen medianoche.

---

# 22. Servicio

Estados actuales definidos:

```text
BORRADOR
PENDIENTE_ASIGNACION
ASIGNADO
PUBLICADO
EN_CURSO
FINALIZADO
CANCELADO
```

No agregar estados nuevos sin una necesidad funcional documentada.

---

# 23. ServicioPasajero

Estados actuales:

```text
PROGRAMADO
CONFIRMADO
NO_ASISTIRA
CONDUCTOR_LLEGO
ESPERANDO
NO_CONTESTA
NO_SE_ENCUENTRA
DIRECCION_INCORRECTA
NO_SE_PUDO_RECOGER
RECOGIDO
EN_VEHICULO
DEJADO_EN_DESTINO
CANCELADO
```

No crear historial automático de estados.

---

# 24. Servicio temporalmente vacío

Durante la reorganización del coordinador puede existir un servicio sin pasajeros.

Esto es permitido temporalmente.

Sin embargo, un servicio no puede publicarse como servicio operativo resuelto si está vacío o tiene problemas de asignación.

---

# 25. Asignación

La asignación de servicios a unidades debe respetar conflictos de solapamiento.

**Decisión vigente (2026-09-30):** una misma unidad no puede tener dos servicios a la misma fecha y hora, salvo una ruta de `ENTRADA` y otra de `SALIDA` de la **misma sede** a la misma hora (el conductor llega a la sede con quienes entran y sale con quienes terminan turno). Dos rutas del mismo tipo, o de sedes distintas, a la misma hora siguen siendo conflicto. La regla vive en `ReglasUnidadOperativa.SeCruzan` y la usan por igual la asignación manual, el reparto automático de la importación y la lista de conductores disponibles del frontend.

No bloquear automáticamente una asignación únicamente por:

* Distancia.
* Carga de trabajo.
* Calidad de ruta.
* Preferencias algorítmicas.

El algoritmo puede recomendar.

El coordinador decide.

---

# 26. Algoritmo de rutas

El algoritmo debe considerar, cuando corresponda:

* Capacidad.
* Hora.
* Fecha.
* Coordenadas.
* Distancia.
* Tiempo estimado.
* Continuidad geográfica.
* Servicio anterior.
* Servicio siguiente.
* Punto de salida.
* Punto de llegada.
* Carga de trabajo.

No utilizar barrios codificados como reglas rígidas.

---

# 27. Importación Excel

El formato exacto del Excel todavía está pendiente.

No inventar columnas definitivas.

No implementar una estructura de Excel definitiva hasta que exista una especificación concreta.

Sí deben respetarse las reglas ya definidas:

* Cédula única.
* No duplicar empleados.
* Varias importaciones pueden complementar la programación.
* No eliminar automáticamente programaciones anteriores.
* Cambios relevantes deben poder revisarse.
* Las importaciones quedan registradas.

---

# 28. ProgramacionTransporte

Representa la necesidad de transporte de un empleado para una fecha y hora determinada.

Mantener separadas:

```text
EmpresaId
EmpleadoId
SedeId
Fecha
Hora
Tipo
DireccionRecogida
BarrioRecogida
```

Una programación puede existir sin estar asignada todavía a un servicio.

Una programación puede asociarse como máximo a un `ServicioPasajero`.

---

# 29. ServicioPasajero y ubicación histórica

Cuando una programación se convierte en pasajero de un servicio, la información concreta utilizada para ese servicio debe quedar almacenada en `ServicioPasajero`.

Esto protege el histórico frente a cambios posteriores del empleado.

---

# 30. Chat

El modelo de comunicación es individual:

```text
ServicioPasajero
    ↓
Conversacion
    ↓
Mensaje
```

La conversación corresponde al conductor y empleado involucrados en ese transporte.

No implementar chat grupal.

---

# 31. Incidencias

Las incidencias pertenecen al pasajero del servicio:

```text
ServicioPasajero
    ↓
Incidencia
    ↓
Evidencia
```

La evidencia debe almacenar una referencia al archivo, no necesariamente el archivo binario dentro de PostgreSQL.

---

# 32. Pruebas

Toda funcionalidad importante debe incluir pruebas.

Antes de marcar una tarea como completada:

1. Compilar.
2. Ejecutar pruebas relevantes.
3. Corregir errores.
4. Verificar regresiones.
5. Documentar cualquier excepción.

---

# 33. No inventar requisitos

Si una especificación no define algo:

No asumir automáticamente.

Determinar primero si:

* Puede resolverse técnicamente sin afectar negocio.
* Requiere una decisión funcional.
* Puede dejarse pendiente.

Si requiere una decisión de negocio, detener esa parte y documentar la duda.

---

# 34. No sobreingenierizar

No introducir automáticamente:

```text
CQRS
MediatR
Event Sourcing
Kafka
RabbitMQ
Redis
Microservicios
Clean Architecture excesivamente compleja
Patrones innecesarios
```

si no existe un requisito que lo justifique.

La primera versión debe mantenerse en un backend ASP.NET Core organizado, mantenible y comprensible.

---

# 35. Migraciones

Toda modificación del modelo de datos debe quedar reflejada mediante migración de Entity Framework Core.

No modificar silenciosamente la estructura de PostgreSQL sin reflejarla en el proyecto.

---

# 36. Git

Los cambios deben ser pequeños y trazables.

Los commits deben describir claramente el cambio.

Evitar mezclar:

```text
Nueva funcionalidad
+
Refactor masivo
+
Cambio de arquitectura
+
Cambios de formato
```

en un único cambio sin necesidad.

---

# 37. Antes de implementar

Antes de comenzar una tarea, Claude debe comprobar:

```text
¿Existe requisito?
¿Existe regla de negocio?
¿Existe entidad relacionada?
¿Existe contrato?
¿Existe tarea?
¿Existe dependencia con otra tarea?
```

Si la tarea depende de una decisión todavía pendiente, no inventarla.

---

# 38. Criterio de finalización

Una tarea solamente puede marcarse como completada cuando sus criterios están verificados.

No marcar:

```text
[X]
```

simplemente porque el código fue escrito.

Debe estar:

```text
Implementado
+
Compilado
+
Probado
+
Verificado
```

cuando corresponda.

---

# 39. Prioridad de las instrucciones

En caso de conflicto, respetar este orden:

```text
1. Requisitos aprobados
2. docs/sdd/00-governance/CONSTITUTION.md
3. docs/sdd/01-specification/spec.md
4. docs/sdd/02-data-model/data-model.md
5. docs/sdd/03-architecture/plan.md
6. docs/sdd/04-execution/tasks.md
7. Decisiones de implementación
```

Nunca utilizar una decisión técnica para contradecir una regla de negocio aprobada.

---

# 40. Objetivo de Claude Code

El objetivo no es simplemente producir código.

El objetivo es producir una implementación:

```text
Correcta
Segura
Probada
Comprensible
Mantenible
Trazable
```

y fiel a las decisiones tomadas en la especificación.

---

# 41. Zona horaria

**Decisión vigente (2026-09-30): la aplicación es para Colombia y todo se expresa en hora de Colombia (UTC−5 fijo, sin horario de verano).** Esta decisión sustituye a la del 2026-09-18 ("toda la plataforma opera en UTC"), que ya no aplica a las fechas y horas programadas.

Hay que distinguir dos clases de datos:

* **Fechas y horas programadas** (`Servicio.Fecha`, `Servicio.HoraProgramada`, `Jornada.FechaOperativa`, `ProgramacionTransporte.Fecha`/`Hora`): se guardan tal como las escribe el coordinador o como vienen en el Excel, **en hora de Colombia**, sin convertir. Toda comparación de estas con "ahora" (por ejemplo, la alerta de ruta no iniciada) usa la hora actual de Colombia: `ReglasHoraColombia.AhoraColombia(DateTime.UtcNow)`, nunca `DateTime.UtcNow` directamente. En el frontend, "hoy" es `hoyColombia()`, no la fecha UTC.
* **Instantes reales** (`HoraInicioReal`, `HoraFinReal`, `HoraProcesado`, `FechaHora` de `Notificacion`/`Mensaje`/`Incidencia`/`Evidencia`, vencimientos de tokens e invitaciones): se guardan en UTC (`DateTime.UtcNow`) y el frontend los muestra en hora local de Colombia.

El desfase de Colombia es fijo (−5 h), por lo que no se usa `TimeZoneInfo` ni ninguna otra lógica de zonas horarias en el backend: la única conversión permitida es `ReglasHoraColombia`.

Si `spec.md` §47, `plan.md` §48 o `data-model.md` §34 todavía dicen que las fechas y horas programadas están en UTC, prevalece esta sección.

---

# 42. Reglas del frontend (React)

Estas reglas son tan obligatorias como las del backend. La arquitectura técnica del frontend (tecnologías, estructura de carpetas, consumo de la API, alcance funcional) ya está definida en `plan.md` §57; esta sección agrega la disciplina de código concreta que debe seguirse al implementarlo.

## 42.1 Idioma

Igual que en el backend (§2): nombres de carpetas, componentes, funciones, variables, props y tipos TypeScript en español, salvo términos técnicos obligatorios del ecosistema (`props`, `hooks`, `useState`, nombres de paquetes npm, atributos HTML/ARIA).

Todo comentario y todo comentario de documentación (JSDoc) también debe estar en español, con el mismo criterio de §3: debe aportar información real (qué representa, responsabilidad principal), nunca un comentario genérico que no diga nada nuevo.

## 42.2 Componentes pequeños, una responsabilidad

Mismo espíritu que §34 (no sobreingenierizar), aplicado al frontend:

* Un componente hace una sola cosa. Si un componente empieza a mezclar layout, llamadas a la API y lógica de formulario compleja, se separa en componentes más pequeños o se extrae la lógica a un hook (`use*`) o a `servicios/`.
* No se crean componentes ni abstracciones (contextos, hooks, wrappers genéricos) para casos hipotéticos todavía no necesarios. Solo lo que la pantalla actual requiere.
* Nada de "componentes gigantes": si un archivo de componente crece hasta mezclar varias pantallas o responsabilidades no relacionadas, se divide.
* La lógica de acceso a la API vive únicamente en `servicios/`, nunca directamente dentro de un componente de página (mismo principio de separación de capas que en el backend: presentación vs. acceso a datos).

## 42.3 CSS separado por componente/página

Cada componente de `componentes/` y cada pantalla de `paginas/` tiene su propio archivo CSS (`NombreComponente.css`), importado solo desde su `.tsx`.

**Decisión vigente (2026-10-01):** los archivos CSS no van junto al `.tsx`, sino en una carpeta aparte que replica la estructura: `src/estilos/componentes/` para los de `componentes/` y `src/estilos/paginas/` para los de `paginas/` (por ejemplo `componentes/BotonTema.tsx` importa `../estilos/componentes/BotonTema.css`). `App.css` vive en `src/estilos/`.

`index.css` se reserva exclusivamente para: reinicio de estilos globales, variables de tema (tokens de color en `:root`) y estilos de elementos raíz (`body`, `#root`). No se agregan ahí estilos específicos de un componente o pantalla concreta.

No se comparte una hoja de estilos monolítica entre múltiples componentes no relacionados.

## 42.4 Solo lo necesario

No se agregan librerías de UI, gestión de estado, CSS-in-JS ni utilidades adicionales sin una necesidad concreta ya existente en la pantalla que se está implementando (coherente con `plan.md` §57.2 y `AGENTS.md` §34). Si el estado local de React (`useState`, `useContext`) alcanza, no se introduce una librería externa para resolverlo.

---

# 43. Tamaño de los archivos

**Decisión vigente (2026-10-01):** ningún archivo de código (clase de backend, componente o página de frontend, hoja de estilos, archivo de pruebas) debe superar las **250 líneas**.

Cuando un archivo se acerca a ese límite se divide por responsabilidad, no de forma arbitraria: en backend, extrayendo colaboradores a `Rules`, `Validators`, `Utils` o servicios más pequeños; en frontend, extrayendo lógica a hooks (`use*`) y bloques de pantalla a subcomponentes (ver §42.2). Dividir un archivo no debe cambiar su comportamiento ni sus contratos públicos.

Quedan fuera del límite las migraciones de Entity Framework Core y el código generado.
