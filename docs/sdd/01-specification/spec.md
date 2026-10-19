# Especificación Funcional — Plataforma de Transporte Empresarial

## 1. Propósito

La Plataforma de Transporte Empresarial permite a empresas gestionar de forma centralizada la programación, asignación, publicación y ejecución de servicios de transporte para sus empleados.

La plataforma debe facilitar:

* gestión multiempresa;
* gestión de empleados;
* gestión de conductores;
* gestión de vehículos;
* gestión de sedes;
* importación de programaciones mediante Excel;
* planificación y organización de rutas;
* asignación de servicios;
* comunicación entre conductores y empleados;
* seguimiento de la ejecución;
* gestión de incidencias;
* notificaciones;
* conservación de información histórica.

La plataforma debe automatizar y asistir al coordinador, pero las decisiones operativas finales permanecen bajo su control.

---

# 2. Principios generales

## 2.1 Multiempresa

La plataforma debe soportar múltiples empresas independientes.

Cada empresa tendrá sus propios:

* empleados;
* coordinadores;
* sedes;
* programaciones;
* jornadas;
* servicios;
* configuración operativa.

Los conductores son independientes y pueden trabajar para varias empresas.

El backend debe garantizar el aislamiento de información entre empresas.

---

# 3. Identidad de las personas

## 3.1 Usuario como identidad global

Toda persona que utilice la plataforma tendrá una única identidad `Usuario`.

La cédula será el identificador único global de la persona.

No se crearán usuarios diferentes para representar diferentes funciones de una misma persona.

Ejemplo:

```text id="t2cy3m"
Usuario
Cédula: 123456789

Puede tener:
- EMPLEADO
- CONDUCTOR
- COORDINADOR
```

---

# 4. Perfiles funcionales

Una persona puede tener uno o ambos perfiles funcionales:

* `Empleado`
* `Conductor`

Estos perfiles representan características funcionales de la persona y no identidades independientes.

Una persona puede ser simultáneamente empleado y conductor.

---

# 5. Sistema de roles

Los roles iniciales son:

* `ADMINISTRADOR_PLATAFORMA`
* `COORDINADOR`
* `CONDUCTOR`
* `EMPLEADO`

Un usuario puede tener múltiples roles.

No se utilizará un único campo `Rol` en `Usuario` para representar todas las posibilidades.

Los roles se gestionarán mediante una relación independiente `UsuarioRol`.

---

# 6. Administrador de plataforma

Existe el rol `ADMINISTRADOR_PLATAFORMA`.

Este rol representa al responsable de la plataforma global.

Sus funciones principales son:

* crear empresas;
* configurar inicialmente una empresa;
* asignar el primer coordinador de cada empresa.

El administrador de plataforma no participa en la operación diaria de las empresas una vez configuradas.

No debe ser necesario que intervenga en:

* empleados;
* conductores;
* vehículos;
* sedes;
* jornadas;
* servicios;
* asignaciones;
* publicaciones;
* incidencias;
* operación diaria.

---

# 7. Primer coordinador de una empresa

Cuando se crea una empresa, el administrador de plataforma debe asignar su primer coordinador.

Flujo:

```text id="d8p3hl"
Administrador de plataforma
          ↓
      Crear empresa
          ↓
  Crear/identificar usuario
          ↓
   Asignar COORDINADOR
          ↓
  Empresa operativamente habilitada
```

El primer coordinador será responsable de administrar la empresa.

---

# 8. Coordinadores adicionales

Una empresa puede tener múltiples coordinadores.

Un coordinador autorizado puede asignar el rol `COORDINADOR` a otros usuarios de su propia empresa.

El usuario seleccionado puede ser:

* empleado;
* conductor;
* empleado y conductor;
* usuario existente autorizado para formar parte de la empresa.

No se debe crear una segunda identidad para esa persona.

Ejemplo:

```text id="gdgq8r"
Usuario Juan
   ├── EMPLEADO
   ├── CONDUCTOR
   └── COORDINADOR
```

El coordinador no puede:

* crear empresas;
* administrar otras empresas;
* asignarse a sí mismo el rol de coordinador;
* tener el rol `COORDINADOR` activo en más de una empresa simultáneamente.

Toda asignación de roles debe ser validada por el backend.

---

# 9. Empleados

La plataforma debe permitir gestionar empleados de cada empresa.

Los empleados pueden ser creados automáticamente mediante la importación de archivos Excel.

La cédula permitirá identificar globalmente a la persona.

Si el empleado ya existe:

* no se debe crear otro usuario;
* no se debe crear otra identidad;
* se debe actualizar la información correspondiente según las reglas definidas.

Un empleado puede cambiar de empresa. Este cambio se detecta y se ejecuta automáticamente durante la importación Excel, cuando la misma cédula aparece asociada a una empresa distinta a la actual (ver §39). No existe una operación manual de transferencia.

El cambio de empresa no debe alterar registros históricos.

---

# 10. Activación de empleados

Cuando un empleado es incorporado mediante Excel, el sistema puede crear automáticamente su usuario.

Inicialmente la cuenta puede no tener contraseña.

La activación inicial se realiza mediante un **código temporal generado exclusivamente por un conductor** (decisión cerrada; ver `data-model.md` §28). No se utiliza WhatsApp, SMS, email ni ningún proveedor externo. El coordinador y el administrador de plataforma no participan en este proceso.

Un conductor solo puede generar el código de un empleado que esté asignado a alguno de sus servicios (mediante `ServicioPasajero`). Esa relación operativa real constituye la autorización; no existe una verificación de identidad adicional. El conductor únicamente genera y entrega el código (presencial o telefónicamente); no puede buscar empleados no asignados a él, activar la cuenta directamente, ni modificar su contraseña o sus datos.

Flujo:

```text id="gmb19o"
Excel
 ↓
Empleado
 ↓
Usuario
 ↓
PasswordHash = NULL
 ↓
Empleado asignado a un Servicio de un Conductor
 ↓
Correo de confirmación / enlace para establecer contraseña (ver AGENTS.md §16: el código generado por un conductor fue eliminado el 2026-09-19)
 ↓
Conductor entrega el código (presencial o telefónico)
 ↓
Activar mi cuenta
 (Cédula + Código + Nueva contraseña + Confirmación)
 ↓
Validación del código (vigencia, uso único, límite de intentos)
 ↓
Crear contraseña
 ↓
Cuenta activa
```

---

# 11. Conductores

Los conductores representan proveedores autónomos del servicio de transporte.

Un conductor puede trabajar para múltiples empresas.

La relación entre conductor y empresa se gestiona mediante una vinculación específica.

Un conductor puede:

* estar activo globalmente;
* estar activo para una empresa;
* estar inactivo para otra empresa.

---

# 12. Vehículos

Un conductor puede tener múltiples vehículos.

Cada vehículo pertenece exclusivamente a un conductor.

Un vehículo no puede ser utilizado temporalmente por otro conductor.

La unidad operativa estará formada por:

```text id="v73d34"
Conductor + Vehículo
```

La unidad operativa será la entidad asignable individualmente a un servicio (no a la jornada; una jornada puede involucrar múltiples unidades operativas a través de sus servicios, ver §17-19).

---

# 13. Sedes

Una empresa puede tener múltiples sedes.

Cada sede debe almacenar:

* nombre;
* dirección;
* ciudad;
* barrio;
* latitud;
* longitud;
* estado activo/inactivo.

Las coordenadas podrán ser opcionales cuando no estén disponibles.

Cada servicio estará asociado a una única sede.

Los pasajeros de diferentes sedes no deben mezclarse dentro del mismo servicio.

---

# 14. Programación de transporte

La programación representa la necesidad concreta de transporte de un empleado.

Cada programación contiene:

* empleado;
* empresa;
* sede;
* fecha;
* hora;
* tipo;
* dirección de recogida;
* barrio.

Los tipos son:

* `ENTRADA`
* `SALIDA`

### Entrada

```text id="x9l6tc"
Empleado → Sede
```

### Salida

```text id="m7xw8b"
Sede → Empleado
```

Una programación puede existir inicialmente sin haber sido asignada a un servicio.

---

# 15. Importación Excel

Los coordinadores podrán importar uno o varios archivos Excel.

El sistema debe:

* identificar empleados;
* crear usuarios cuando corresponda;
* crear o actualizar perfiles;
* crear programaciones;
* detectar registros nuevos;
* detectar registros idénticos;
* detectar posibles cambios;
* evitar eliminar automáticamente información anterior.

Si una importación contiene un registro exactamente igual a uno existente, el sistema debe considerarlo sin cambios.

Si contiene información diferente relevante, debe informar al coordinador y permitir decidir si actualizar o conservar la información existente.

Cada importación registra un estado (`ImportacionExcel.Estado`) con los valores definidos en `data-model.md` §13.3: `PENDIENTE`, `PROCESANDO`, `COMPLETADA`, `COMPLETADA_CON_ADVERTENCIAS`, `ERROR`.

El formato exacto de Excel queda pendiente hasta disponer del archivo real.

La plataforma no debe inventar columnas o reglas que no hayan sido definidas a partir del archivo real.

**Decisión del usuario (2026-10-02): importación sobre rutas ya publicadas o en curso.** Una importación nunca agrega pasajeros a una ruta que ya está en curso, finalizada o cancelada: esos pasajeros van a otra ruta. Si completa una ruta que ya estaba publicada, la ruta vuelve a `ASIGNADO` (el conductor deja de verla) y el resultado de la importación lo advierte; al volver a publicarla, el conductor recibe el aviso y el correo con el Excel actualizado.

---

# 16. Historial de ubicación

El sistema debe permitir conservar ubicaciones de recogida utilizadas anteriormente por un empleado.

Debe diferenciar:

```text id="h9ocx5"
Empleado
→ ubicación actual/default

UbicacionRecogidaHistorica
→ ubicaciones anteriores

ServicioPasajero
→ ubicación exacta utilizada en ese servicio
```

Si el empleado proporciona una nueva ubicación, el sistema podrá preguntar si desea establecerla como ubicación habitual para futuras programaciones.

---

# 17. Jornadas

Una jornada representa el bloque operativo de transporte de una empresa para una fecha operativa determinada. No pertenece a una única unidad operativa.

Una jornada contiene uno o más servicios, y puede involucrar múltiples unidades operativas distintas: cada servicio se asigna individualmente a la suya (ver §19). Una jornada puede así atender a cientos de empleados mediante múltiples conductores y vehículos.

La jornada tiene:

* empresa;
* fecha operativa.

La jornada no tiene hora de inicio ni hora de finalización.

La fecha operativa es una fecha de referencia y puede contener servicios de fechas calendario diferentes.

Ejemplo:

```text id="fkwy9q"
Jornada
FechaOperativa: 18/09

17/09 22:00 → SALIDA
18/09 00:00 → ENTRADA
18/09 04:00 → ENTRADA
```

---

# 18. Servicios

Un servicio representa una operación concreta de transporte.

Debe contener:

* jornada;
* unidad operativa (puede estar temporalmente sin asignar durante la planificación, ver §19);
* sede;
* fecha;
* hora programada;
* tipo;
* estado;
* hora real de inicio;
* hora real de finalización.

La fecha y hora programadas se mantienen como campos separados.

Los estados son:

* `BORRADOR`
* `PENDIENTE_ASIGNACION`
* `ASIGNADO`
* `PUBLICADO`
* `EN_CURSO`
* `FINALIZADO`
* `CANCELADO`

---

# 19. Asignación de servicios

Cada servicio se asigna individualmente a una unidad operativa. Dos servicios de la misma jornada pueden tener unidades operativas distintas.

Un coordinador puede redistribuir servicios entre unidades. Reasignar la unidad operativa de un servicio no cambia la jornada a la que pertenece.

El sistema debe impedir únicamente asignaciones incompatibles, especialmente cuando exista solapamiento temporal que haga imposible atender dos servicios simultáneamente.

El sistema no debe bloquear una asignación únicamente por:

* distancia;
* carga de trabajo;
* calidad estimada de ruta;
* preferencias del algoritmo.

Estas situaciones pueden ser advertencias o recomendaciones, pero la decisión final corresponde al coordinador.

---

# 20. Publicación de servicios

La publicación representa el momento en que la planificación se comunica a los usuarios involucrados.

Antes de publicar:

* los servicios deben estar resueltos;
* cada servicio debe tener unidad asignada;
* no deben existir inconsistencias de empresa;
* los servicios no pueden quedar sin resolución.

Una jornada con servicios pendientes de asignación no debe publicarse.

Después de publicar, el coordinador puede realizar modificaciones.

Si una modificación afecta directamente a determinados usuarios, solo ellos deben recibir la notificación correspondiente.

**Decisión del usuario (2026-10-02): soporte de rutas por correo.** Al publicar, cada conductor al que se le publicó alguna ruta recibe además, en el correo de su cuenta, un archivo Excel de soporte para consultar sus rutas si la aplicación no está disponible:

* contiene únicamente **sus propias rutas** de esa jornada que ya son visibles para él (nunca las de otros conductores ni sus pasajeros);
* es una sola hoja con un bloque por ruta (sentido, sede, fecha y hora) y, debajo, sus pasajeros en orden de recogida con cédula, nombre, teléfono, dirección y barrio;
* cada nueva publicación reenvía el soporte actualizado; el correo más reciente es el vigente;
* si el conductor no tiene correo o el envío falla, la publicación no se deshace.

**Decisión del usuario (2026-10-02): Excel de la jornada para el coordinador.** Desde la pantalla de Programación, el coordinador puede descargar un Excel con el mismo formato que reúne **todas** las rutas no canceladas de la jornada (publicadas o no, de todos los conductores y las que aún no tienen conductor), indicando en cada bloque el conductor de la ruta. Es solo una descarga: no publica ni envía nada.

---

# 21. Servicio pasajero

Cada empleado que participa en un servicio estará representado mediante `ServicioPasajero`.

Debe conservar:

* servicio;
* programación;
* empleado;
* estado;
* orden;
* dirección de recogida;
* coordenadas.

El conductor puede modificar el orden de los pasajeros.

El orden modificado debe conservarse.

---

# 22. Estados del pasajero

Los estados disponibles son:

* `PROGRAMADO`
* `CONFIRMADO`
* `NO_ASISTIRA`
* `CONDUCTOR_LLEGO`
* `RECOGIDO`
* `NO_RECOGIDO`
* `CANCELADO`

**Decisión del usuario (2026-09-19):** el conductor solo registra dos pasos por pasajero: la llegada al punto de recogida (`CONDUCTOR_LLEGO`) y el resultado final (`RECOGIDO` o el tipo de incidencia). Se eliminaron `ESPERANDO`, `EN_VEHICULO` y `DEJADO_EN_DESTINO`: un pasajero `RECOGIDO` se considera transportado cuando el servicio se finaliza, sin acción adicional del conductor.

**Decisión del usuario (2026-09-19, ajuste):** los motivos por los que no se recoge a un pasajero (`NO_CONTESTA`, `NO_SE_ENCUENTRA`, `DIRECCION_INCORRECTA`, `NO_SE_PUDO_RECOGER`) ya no son estados: se registran como **incidencias** (con foto y ubicación). Una incidencia de esos cuatro tipos deja al pasajero automáticamente como `NO_RECOGIDO`. `UBICACION_MODIFICADA` y `OTRA` son informativas y no cambian el estado. **Ajuste del 2026-10-02:** ya no hay incidencias solo informativas; una incidencia de cualquier tipo (también `UBICACION_MODIFICADA` y `OTRA`) deja al pasajero como `NO_RECOGIDO`. Además, tampoco una ruta de `SALIDA` puede finalizarse con pasajeros pendientes de procesar.

No se implementará inicialmente un historial automático de todos los cambios de estado.

El sistema conservará el estado actual.

---

# 23. Confirmación del empleado

El empleado debe poder:

* consultar su servicio;
* confirmar asistencia;
* indicar que no asistirá;
* comunicarse con el conductor;
* compartir ubicación cuando sea necesario.

Si indica:

```text id="4gj7f2"
NO_ASISTIRA
```

el conductor debe recibir la información.

El pasajero debe dejar de bloquear la operación normal del conductor.

El servicio podrá finalizar aunque un pasajero haya indicado que no asistirá.

---

# 24. Ejecución de servicios

El conductor debe poder iniciar manualmente una ruta.

El sistema registra la hora real de inicio.

Durante la ejecución el conductor podrá:

* consultar pasajeros;
* navegar hacia ubicaciones;
* detectar llegada;
* registrar espera;
* llamar al empleado;
* utilizar chat;
* registrar incidencias;
* tomar evidencias;
* modificar orden;
* procesar pasajeros;
* finalizar el servicio.

---

# 25. Tiempo de espera

El tiempo máximo operativo inicialmente establecido es de aproximadamente 2 minutos.

La aplicación debe permitir al conductor gestionar el tiempo de espera del pasajero.

El sistema podrá generar estados o incidencias según el resultado.

---

# 26. Geolocalización

La aplicación podrá utilizar geolocalización para:

* detectar llegada;
* registrar posición;
* facilitar navegación;
* respaldar incidencias;
* permitir reglas de geocerca.

Debe existir una alternativa manual cuando la detección automática no sea posible o confiable.

El radio exacto de geocerca y proveedor tecnológico quedan pendientes.

---

# 27. Finalización de servicios

Para servicios de entrada:

* el servicio termina cuando la operación se completa en la sede;
* no debe permitirse finalizar manualmente mientras existan pasajeros pendientes;
* podrá existir finalización automática mediante geolocalización bajo reglas que se definirán posteriormente.

Para servicios de salida:

* pueden finalizar cuando se complete el transporte;
* no necesitan terminar en una ubicación concreta.

---

# 28. Planificación automática asistida

La plataforma debe proporcionar recomendaciones de planificación.

El sistema debe considerar:

* capacidad;
* horario;
* ubicación;
* continuidad geográfica;
* tiempo estimado de desplazamiento;
* tiempo conjunto aproximado de recogida;
* llegada a la sede al menos 15 minutos antes;
* ubicación del servicio anterior;
* ubicación del siguiente servicio;
* carga de trabajo.

Para entradas se utilizará como heurística general:

```text id="ubpsg3"
Pasajero más lejano
        ↓
Pasajeros progresivamente más cercanos
        ↓
Sede
```

El algoritmo debe generar una propuesta.

El coordinador conserva la decisión final.

La automatización no puede sobrescribir unilateralmente una decisión del coordinador.

---

# 29. Capacidad

El algoritmo y las validaciones de asignación deben considerar la capacidad del vehículo.

No se debe permitir una asignación que supere la capacidad operacional definida del vehículo.

---

# 30. Continuidad entre servicios

La planificación debe considerar también la relación entre servicios consecutivos.

Cuando sea posible, debe evitarse que una unidad termine un servicio y tenga que desplazarse innecesariamente a una ubicación lejana para comenzar el siguiente.

El sistema debe considerar:

```text id="hknlq6"
Fin servicio anterior
        ↓
Primer pasajero siguiente
        ↓
Sede siguiente
```

La consideración será parte de la recomendación y no debe convertirse automáticamente en una prohibición salvo que exista un conflicto temporal real.

---

# 31. Alerta de ruta no iniciada

Para servicios de entrada se debe contemplar una alerta previa.

Regla inicial:

```text id="m4a7t2"
1 hora antes de la entrada
        ↓
¿Ruta iniciada?
        ↓
NO
        ↓
Notificar conductor responsable
```

Ejemplo:

```text id="4n0qz8"
Entrada: 01:00
Alerta aproximada: 00:00
```

La hora exacta de inicio recomendada podrá depender posteriormente del algoritmo de planificación.

---

# 32. Incidencias

El conductor debe poder registrar incidencias asociadas a un pasajero.

Tipos iniciales:

* `NO_CONTESTA`
* `NO_SE_ENCUENTRA`
* `DIRECCION_INCORRECTA`
* `NO_SE_PUDO_RECOGER`
* `UBICACION_MODIFICADA`
* `OTRA`

Cada incidencia puede contener:

* descripción;
* fecha y hora;
* ubicación;
* evidencias.

---

# 33. Evidencias

El conductor podrá asociar evidencias a una incidencia.

La plataforma admite inicialmente un único tipo de evidencia:

* `FOTOGRAFIA`

No se contemplan inicialmente otros tipos de archivo. Cualquier tipo adicional requerirá una decisión explícita antes de implementarse.

La base de datos conservará una referencia al archivo.

No se almacenarán inicialmente los archivos binarios directamente dentro de PostgreSQL.

---

# 34. Chat

La comunicación será individual entre conductor y empleado.

Cada `ServicioPasajero` podrá tener una conversación.

La conversación contiene mensajes.

No se implementará inicialmente:

* chat grupal;
* canales generales;
* grabación de llamadas.

Las llamadas se realizarán utilizando las capacidades del dispositivo.

---

# 35. Notificaciones

La plataforma debe proporcionar notificaciones a los usuarios cuando corresponda.

Ejemplos:

* publicación de servicio;
* cambio de servicio;
* cambio de conductor;
* cambio de horario;
* incidencias relevantes;
* alertas de ruta no iniciada;
* confirmaciones;
* modificaciones operativas.

Las modificaciones deben notificarse a los usuarios directamente afectados.

---

# 36. Seguridad

La seguridad debe aplicarse en backend.

El frontend no debe ser considerado un mecanismo suficiente para proteger datos.

El backend debe comprobar:

* usuario autenticado;
* rol;
* rol activo;
* empresa;
* relación con la entidad;
* permisos de operación.

Un usuario no puede consultar información de otra empresa solamente modificando un `EmpresaId` enviado en la petición.

---

# 37. Control de acceso

### Administrador de plataforma

Puede:

* gestionar empresas;
* configurar inicialmente empresas;
* asignar primer coordinador.

### Coordinador

Puede gestionar la operación de su empresa.

Puede además:

* crear/asignar coordinadores adicionales de su propia empresa;
* gestionar empleados de su propia empresa (consultar, actualizar, activar/desactivar). El cambio de empresa de un empleado no es una operación que el coordinador ejecute manualmente; se resuelve automáticamente durante la importación Excel (ver §39);
* gestionar conductores;
* gestionar vehículos;
* gestionar sedes;
* gestionar programaciones;
* gestionar jornadas;
* gestionar servicios;
* publicar;
* gestionar incidencias y operación.

### Conductor

Puede acceder únicamente a:

* sus servicios;
* sus jornadas;
* pasajeros correspondientes;
* navegación;
* ejecución;
* incidencias;
* chat permitido;
* información necesaria para realizar el transporte.

### Empleado

Puede acceder únicamente a:

* su información;
* sus programaciones;
* sus servicios;
* su información de transporte;
* confirmación/no asistencia;
* chat;
* ubicación cuando corresponda.

---

# 38. Conservación histórica

La plataforma no debe eliminar físicamente información histórica necesaria para comprender operaciones anteriores.

Cuando una entidad deje de utilizarse debe preferirse la desactivación.

Esto aplica especialmente a:

* empleados;
* conductores;
* vehículos;
* sedes;
* empresas;
* usuarios;
* unidades operativas.

Los servicios y registros históricos deben conservarse.

---

# 39. Cambio de empresa de empleado

No existe una operación manual de transferencia de un empleado entre empresas.

El cambio de empresa se detecta y se ejecuta automáticamente durante la importación de un archivo Excel, cuando la misma cédula (identificada mediante `Usuario`) aparece asociada a una empresa distinta a la actual. `ADMINISTRADOR_PLATAFORMA` no participa en este proceso ni en la gestión operativa de empleados.

Al detectarse el cambio, deben actualizarse en la misma transacción `Empleado.EmpresaId` y el `EmpresaId` del `UsuarioRol` del rol `EMPLEADO` correspondiente.

Si una persona pasa de una empresa a otra:

```text id="h9c5t8"
Misma cédula
       ↓
Mismo Usuario
       ↓
Mismo perfil Empleado
       ↓
Nueva Empresa actual
```

No se crea un segundo usuario.

Los registros históricos mantienen el contexto empresarial original.

Una empresa nueva no obtiene automáticamente acceso a la información histórica de la persona en otra empresa.

**Decisión del usuario (2026-10-02): eliminar a una persona desde la gestión de empleados.** Un `COORDINADOR` puede eliminar a un empleado de su empresa para corregir registros erróneos y permitir que la persona vuelva a registrarse. Si la persona solo pertenece a esa empresa, se borra todo rastro de su cédula: cuenta, roles, ficha de conductor con sus vehículos y unidades, invitaciones y también su historial (rutas en las que fue pasajera, mensajes, incidencias y evidencias); es una excepción expresa a la regla de no modificar el histórico. Las rutas que conducía se conservan, pero quedan sin unidad (las asignadas o publicadas vuelven a `PENDIENTE_ASIGNACION`). Si la persona también pertenece a otra empresa (rol, vinculación como conductora o invitación aceptada), solo se la quita de la empresa del coordinador y su cuenta se conserva. No se puede eliminar a un coordinador o administrador (hay que quitarle antes el rol), a quien tiene una ruta en curso, ni a uno mismo. El coordinador también puede quitar cualquier invitación de la lista de invitaciones enviadas; quitar una pendiente anula su enlace. Ver `EliminacionPersonaServicio`.

**Decisión del usuario (2026-10-03): eliminar la propia cuenta.** Cualquier persona, sea cual sea su rol (también un coordinador), puede eliminar su propia cuenta desde «Mi cuenta», confirmando con su contraseña; es además un requisito de las tiendas de aplicaciones. Se borra siempre la cuenta completa, con las mismas consecuencias descritas arriba. Si una empresa queda sin coordinador, el administrador de plataforma le asigna uno nuevo. No puede hacerlo quien tiene una ruta en curso ni el único administrador de plataforma (la plataforma quedaría sin nadie que la administre). Para que esto sea posible, las importaciones de Excel y las invitaciones conservan su registro aunque desaparezca quien las hizo: `ImportacionExcel.CoordinadorId` e `InvitacionEmpresa.UsuarioInvitadorId` pasan a ser opcionales y quedan vacíos; por lo mismo, un coordinador ya puede eliminar a un antiguo coordinador que dejó esos registros. La web publica dos páginas sin sesión: `/privacidad` (política de tratamiento de datos, responsable Luis Fernando Ramírez Castaño) y `/eliminar-cuenta` (cómo eliminar la cuenta y qué se borra).

**Decisión del usuario (2026-10-05): facturación por conductor.** La plataforma se cobra por mes y por conductor que **finalizó al menos una ruta** en ese mes para la empresa; un conductor que trabaja para dos empresas cuenta en cada una. El mes se determina por el día en que se finalizó la ruta, en hora de Colombia. Al finalizar una ruta se anota un registro (`UsoConductor`) con una copia de los datos del conductor (cédula, nombre, placa) y los pasajeros recogidos; ese registro no se modifica ni se borra, tampoco cuando se elimina al conductor, de modo que este sigue contando en el mes en que trabajó y deja de aparecer desde el siguiente. A una misma persona se la identifica por su cédula. El administrador de plataforma dispone de una pantalla de Facturación, **de solo lectura sobre la operación**, con el resumen por empresa y mes, el detalle por conductor, un Excel de soporte y el **cierre mensual** (`CierreMensual`), que deja fijos los totales y solo puede hacerse una vez y sobre un mes ya terminado. La pantalla no maneja precios ni genera facturas. Es una excepción acotada a la regla de que el administrador no participa en la gestión operativa: ve nombres y cédulas de conductores únicamente para facturar. Ver `FacturacionServicio`.

**Decisión del usuario (2026-10-05): corregir y eliminar empresas.** El administrador de plataforma puede corregir el nombre, el CIF y la dirección de una empresa, y eliminarla por completo. Eliminar borra la empresa con **todo su historial**, también el de facturación: sedes, zonas, macrozonas, corredores, barreras, jornadas, rutas, pasajeros, chats, incidencias y evidencias, programaciones, importaciones, invitaciones, registros de uso y cierres mensuales. Las personas **conservan su cuenta**, ya sin empresa: el rol `EMPLEADO` queda sin empresa, el de `COORDINADOR` de esa empresa desaparece y los conductores conservan su ficha, sus vehículos y sus unidades (pierden solo la vinculación). Se borran únicamente las cuentas que nunca se activaron (sin contraseña) y no tienen otra relación, normalmente pasajeros creados por una importación. No se puede eliminar una empresa con una ruta en curso. Es una excepción expresa a la regla de no modificar el histórico. Ver `EdicionEmpresaServicio`.

**Decisión del usuario (2026-10-06): registro de llamadas del conductor al pasajero.** Para poder comprobar si el conductor llamó a un pasajero, cada vez que pulsa "Llamar" en la tarjeta del pasajero se anota un `RegistroLlamada` con el instante (UTC, puesto por el servidor), la ubicación del conductor si está disponible y una **duración aproximada**: el tiempo que estuvo fuera de la aplicación hasta volver, que incluye lo que timbró. La llamada ocurre en el marcador del teléfono, así que el registro **no indica si el pasajero contestó**; no se lee el historial de llamadas del dispositivo ni se usa una central telefónica. Una duración de más de 30 minutos se descarta por inverosímil y la duración se guarda una sola vez. El botón "Llamar" abre dos opciones: llamar y ver el historial de llamadas. El historial lo ven **los tres implicados**: el conductor asignado, el empleado que es el pasajero y los coordinadores de la empresa (en el detalle de la ruta), y se muestra además dentro de la incidencia de tipo "No contesta". Solo el conductor asignado puede registrar llamadas. Ver `RegistroLlamadaServicio`.

---

# 40. Reglas de integridad

El backend debe validar:

### Empresa

* sede pertenece a empresa;
* empleado pertenece a empresa;
* programación pertenece a empresa;
* jornada pertenece a empresa.

### Servicio

* servicio pertenece a la jornada;
* sede pertenece a la misma empresa;
* unidad operativa (cuando está asignada) pertenece, mediante su conductor, a una vinculación activa con la misma empresa de la jornada;
* pasajeros pertenecen a la programación correspondiente.

### Unidad

```text id="mbqv4q"
Vehículo.ConductorId
==
UnidadOperativa.ConductorId
```

### Identidad

```text id="p6nd6y"
Usuario.Cedula
```

debe ser única globalmente.

Los perfiles `Empleado` y `Conductor` deben referenciar al `Usuario` correspondiente.

---

# 41. API

La primera versión debe exponer una API REST para permitir posteriormente la integración con:

* aplicación web;
* aplicación móvil;
* otros clientes autorizados.

La API debe utilizar JSON.

Swagger/OpenAPI documentará los contratos.

---

# 42. Aplicación frontend

El frontend será responsable de:

* presentación;
* navegación;
* interacción del usuario;
* visualización de servicios;
* formularios;
* mapas;
* notificaciones;
* selección de rol/contexto cuando corresponda.

El frontend no debe implementar como única defensa las reglas de seguridad.

Todas las reglas críticas deben validarse también en backend.

---

# 43. Persistencia

La plataforma utilizará:

* PostgreSQL;
* Entity Framework Core;
* migraciones.

Las relaciones y restricciones importantes deben estar respaldadas por la base de datos cuando sea técnicamente posible.

---

# 44. Pruebas

La plataforma debe incluir pruebas unitarias y de integración.

Las pruebas deben cubrir como mínimo:

* autenticación;
* roles;
* permisos;
* múltiples roles;
* aislamiento entre empresas;
* creación de empresas;
* primer coordinador;
* creación de coordinadores adicionales;
* empleados;
* conductores;
* vehículos;
* unidades;
* importación;
* programaciones;
* jornadas;
* servicios;
* pasajeros;
* estados;
* planificación;
* publicación;
* ejecución;
* incidencias;
* chat.

---

# 45. Tecnologías

La primera versión utilizará:

* C#
* ASP.NET Core Web API
* Entity Framework Core
* PostgreSQL
* JWT
* Swagger/OpenAPI
* Docker
* xUnit
* Git
* GitHub

La aplicación será inicialmente un monolito modular.

No se implementarán microservicios en la primera versión.

---

# 46. Fuera de alcance inicial

Quedan fuera de la primera versión:

* OAuth;
* chat grupal;
* grabación de llamadas;
* historial completo de estados;
* Event Sourcing;
* CQRS;
* microservicios;
* IA para decisiones autónomas;
* optimización avanzada de rutas mediante IA;
* mensajería externa;
* historial independiente completo de cambios de empresa del empleado;
* automatización que sustituya la decisión del coordinador.

---

# 47. Decisiones pendientes

Las siguientes decisiones todavía no están cerradas y no deben ser inventadas durante la implementación:

1. Formato exacto del Excel.
2. Proveedor de geocodificación.
3. Proveedor de mapas.
4. Proveedor de cálculo de rutas.
5. Radio exacto de geocerca.
6. Fórmula exacta del algoritmo de planificación.
7. Pesos de los criterios de planificación.
8. Hora exacta recomendada de inicio.
9. Estrategia definitiva de selección/cambio de rol activo.
10. Política de expiración y renovación JWT.
11. Almacenamiento definitivo de evidencias.
12. Política detallada de edición después de publicación.
13. Auditoría de asignación y eliminación de roles.
14. Reglas detalladas para creación de coordinadores adicionales.
15. Índices definitivos según consultas reales.

El método de activación inicial (código generado por el conductor) ya no es una decisión pendiente: quedó cerrado en `data-model.md` §28 y en esta sección §10.

La estrategia de zona horaria ya no es una decisión pendiente (decisión vigente, 2026-09-30, sustituye a la del 2026-09-18 que ponía todo en UTC): la aplicación opera en hora de Colombia (UTC−5 fijo): las fechas y horas programadas (`Fecha`, `HoraProgramada`, `FechaOperativa`) se guardan y comparan en hora de Colombia, y los instantes reales (`HoraInicioReal`, `HoraFinReal`, `FechaHora`) se guardan en UTC y el frontend los muestra en hora de Colombia. Ver `AGENTS.md` §41.

La estrategia definitiva de claims JWT ya no es una decisión pendiente (decisión cerrada, 2026-09-18): cada rol activo del usuario se codifica como un claim `rol` independiente, con formato `"ROL:EmpresaId"` para roles con empresa (`COORDINADOR`, `EMPLEADO`) o `"ROL"` para roles globales (`CONDUCTOR`, `ADMINISTRADOR_PLATAFORMA`). Ver `GeneradorTokenJwt` (`LFMova.Infrastructure.Autenticacion`) y `docs/sdd/03-architecture/overview.md` §4.

La estrategia definitiva para pruebas de integración ya no es una decisión pendiente (decisión cerrada, 2026-09-18): un único contenedor PostgreSQL (Testcontainers.PostgreSql, imagen `postgres:16-alpine`) compartido por colección xUnit, migrado una sola vez, ejercitado mediante `WebApplicationFactory` con HTTP real. Ver `tasks.md` T114-T118.

---

# 48. Criterios generales de aceptación

La primera versión será considerada funcional cuando:

* una empresa pueda ser creada por el administrador de plataforma;
* pueda asignarse su primer coordinador;
* el coordinador pueda administrar autónomamente su empresa;
* el coordinador pueda agregar otros coordinadores de su empresa;
* una persona pueda tener múltiples roles;
* una persona pueda ser simultáneamente empleado y conductor;
* la cédula sea única globalmente;
* exista aislamiento entre empresas;
* puedan gestionarse empleados y conductores;
* puedan gestionarse vehículos y unidades;
* puedan importarse programaciones;
* puedan crearse jornadas;
* puedan crearse y asignarse servicios;
* puedan publicarse servicios resueltos;
* los empleados puedan consultar y confirmar sus servicios;
* los conductores puedan ejecutar servicios;
* puedan registrarse incidencias y evidencias;
* exista comunicación individual;
* existan notificaciones;
* exista una primera versión de planificación asistida;
* las reglas críticas sean validadas por backend;
* existan pruebas suficientes para las funcionalidades principales.

---

# 49. Principio final

La plataforma debe ser una herramienta que permita a cada empresa gestionar autónomamente su operación de transporte.

La responsabilidad se distribuye de la siguiente manera:

```text id="7w6i3q"
ADMINISTRADOR DE PLATAFORMA
        │
        │ configuración inicial
        ▼
     EMPRESA
        │
        ▼
   COORDINADORES
        │
        ├── Empleados
        ├── Conductores
        ├── Vehículos
        ├── Sedes
        ├── Programaciones
        ├── Jornadas
        ├── Servicios
        └── Operación
```

El administrador de plataforma crea la empresa y su primer coordinador.

A partir de ese momento, la empresa debe poder operar de manera autónoma mediante sus coordinadores.

La plataforma debe mantener siempre:

* identidad única;
* múltiples roles;
* seguridad multiempresa;
* conservación histórica;
* reglas de negocio en backend;
* intervención humana del coordinador en decisiones operativas;
* arquitectura sencilla y mantenible.
