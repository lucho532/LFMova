# Checklist — Plataforma de Gestión de Transporte

## 1. Propósito

Este checklist define las verificaciones necesarias para determinar si la implementación cumple con:

* `CONSTITUTION.md`
* `AGENTS.md`
* `spec.md`
* `data-model.md`
* `plan.md`
* `tasks.md`

No reemplaza las tareas de implementación.

Una tarea puede estar marcada como `[X]` en `tasks.md` y aun así requerir corrección si falla alguna verificación de este documento.

---

# 2. Estados del checklist

Utilizar:

* `[ ]` Pendiente
* `[X]` Cumplido
* `[!]` Incumplido
* `[~]` En revisión
* `[-]` No aplica, con justificación

No marcar `[X]` solamente porque el código exista.

---

# 3. Arquitectura

## Estructura

* [ ] Existe solución .NET funcional.
* [ ] Existe `TransportApp.Api`.
* [ ] Existe `TransportApp.Application`.
* [ ] Existe `TransportApp.Domain`.
* [ ] Existe `TransportApp.Infrastructure`.
* [ ] Existe proyecto de pruebas unitarias.
* [ ] Existe proyecto de pruebas de integración.

## Dependencias

* [ ] Api depende de Application.
* [ ] Api depende de Infrastructure únicamente donde corresponde.
* [ ] Application depende de Domain.
* [ ] Infrastructure depende de Application/Domain.
* [ ] Domain no depende de Infrastructure.
* [ ] Domain no depende de Api.
* [ ] Application no depende directamente de PostgreSQL.
* [ ] Controllers no acceden directamente a `DbContext`.

## Responsabilidades

* [ ] Controllers solamente manejan HTTP y coordinación de la petición.
* [ ] La lógica de negocio está fuera de Controllers.
* [ ] Services contienen los casos de uso.
* [ ] Repositories manejan persistencia.
* [ ] DTOs no se utilizan como entidades de dominio.
* [ ] Entidades EF no se exponen directamente como respuesta de API.
* [ ] Mappers realizan las conversiones correspondientes.
* [ ] Validators realizan validaciones de entrada.
* [ ] Rules contienen reglas de dominio cuando corresponda.

---

# 4. Nomenclatura y código

* [ ] Clases en español.
* [ ] Interfaces en español.
* [ ] Métodos en español.
* [ ] Propiedades en español.
* [ ] Variables en español.
* [ ] Enums en español.
* [ ] DTOs en español.
* [ ] Services en español.
* [ ] Repositories en español.
* [ ] Comentarios relevantes en español.
* [ ] Documentación XML en español.
* [ ] No existen nombres arbitrarios en inglés para conceptos del dominio.
* [ ] No existen abreviaturas innecesarias.
* [ ] Las clases tienen una responsabilidad clara.
* [ ] No existen clases gigantes con múltiples responsabilidades.

---

# 5. Identidad global

## Usuario

* [ ] Existe una única entidad `Usuario`.
* [ ] `Cedula` es globalmente única.
* [ ] `Usuario` no contiene `Rol`.
* [ ] `Usuario` no contiene `EmpresaId`.
* [ ] `PasswordHash` puede ser nulo.
* [ ] `Activo` permite desactivar cuentas sin eliminarlas.

## Múltiples roles

* [ ] Existe `UsuarioRol`.
* [ ] Un Usuario puede tener múltiples roles.
* [ ] `EmpresaId` es obligatorio en `UsuarioRol` para `COORDINADOR` y `EMPLEADO`.
* [ ] `EmpresaId` es `NULL` en `UsuarioRol` para `ADMINISTRADOR_PLATAFORMA` y para `CONDUCTOR`.
* [ ] No se crea un `UsuarioRol CONDUCTOR` por cada empresa vinculada.
* [ ] `ADMINISTRADOR_PLATAFORMA` puede ser global.
* [ ] Un usuario no puede autoasignarse un rol.
* [ ] Un usuario no puede tener el rol `COORDINADOR` activo en más de una empresa simultáneamente.
* [ ] Una empresa nunca queda sin ningún `COORDINADOR` activo.
* [ ] Existe operación de revocación de `UsuarioRol` con las mismas validaciones que la asignación.
* [ ] No se crean usuarios duplicados para representar diferentes roles.

## Perfiles

* [ ] Usuario puede tener perfil Empleado.
* [ ] Usuario puede tener perfil Conductor.
* [ ] Usuario puede tener ambos perfiles simultáneamente.
* [ ] Empleado y Conductor no representan identidades independientes.
* [ ] No existe una entidad innecesaria `Coordinador`.
* [ ] No existe una entidad innecesaria `AdministradorPlataforma`.

---

# 6. Empresas y seguridad multiempresa

* [ ] Existe aislamiento lógico entre empresas.
* [ ] Un coordinador solamente puede operar sobre su empresa.
* [ ] Un coordinador no puede crear empresas.
* [ ] Un coordinador puede gestionar coordinadores de su propia empresa.
* [ ] Un coordinador no puede administrar otra empresa.
* [ ] Administrador de plataforma puede crear empresas.
* [ ] Administrador de plataforma realiza principalmente configuración inicial.
* [ ] Administrador de plataforma no gestiona ni conoce la operativa o el personal de las empresas (incluyendo el cambio de empresa de un empleado).
* [ ] El backend valida el contexto de empresa.
* [ ] La seguridad no depende únicamente de React.
* [ ] Un usuario no puede consultar datos de otra empresa modificando un ID en una petición.
* [ ] Las consultas sensibles aplican filtros de empresa.
* [ ] Se validan relaciones entre entidades de diferentes empresas.

---

# 7. Empresa

* [ ] `Empresa` contiene solamente los campos definidos.
* [ ] `Nombre` es obligatorio.
* [ ] `Activa` controla disponibilidad.
* [ ] No se agregaron campos no definidos sin justificación SDD.

---

# 8. Sede

* [ ] Cada sede pertenece a una empresa.
* [ ] Una empresa puede tener múltiples sedes.
* [ ] Sede contiene dirección.
* [ ] Sede contiene ciudad.
* [ ] Sede contiene barrio.
* [ ] Coordenadas pueden ser nulas.
* [ ] Una sede inactiva permanece disponible para históricos.
* [ ] Una sede inactiva no se ofrece para nueva programación.
* [ ] Un servicio solamente utiliza una sede.
* [ ] No se mezclan pasajeros de diferentes sedes dentro de un mismo servicio.

---

# 9. Empleado

* [ ] Empleado referencia Usuario mediante `UsuarioId`.
* [ ] Empleado referencia Empresa.
* [ ] La cédula no está duplicada en Empleado.
* [ ] Existe consulta de empleados filtrada por la empresa del coordinador autenticado.
* [ ] Existe actualización de datos actuales del empleado (nombre, teléfono, dirección, barrio) sin alterar históricos.
* [ ] Existe activación/desactivación directa de empleado.
* [ ] El empleado puede cambiar de empresa.
* [ ] No existe ninguna operación manual de transferencia de empleado entre empresas (ni para `COORDINADOR` ni para `ADMINISTRADOR_PLATAFORMA`).
* [ ] El cambio de empresa se detecta y se ejecuta automáticamente durante la importación Excel, al encontrar la misma cédula en otra empresa.
* [ ] `Empleado.EmpresaId` y `UsuarioRol(EMPLEADO).EmpresaId` se actualizan en la misma transacción al detectarse el cambio.
* [ ] El cambio de empresa no modifica históricos.
* [ ] Se conserva la dirección actual del empleado.
* [ ] Las ubicaciones anteriores pueden almacenarse históricamente.
* [ ] `UbicacionRecogidaHistorica` incluye `FechaRegistro`.
* [ ] El empleado puede decidir conservar una nueva dirección como ubicación habitual sin alterar `ServicioPasajero` ya registrados.
* [ ] No se eliminan registros históricos por cambio de empresa.

---

# 10. Conductor

* [ ] Conductor referencia Usuario.
* [ ] Conductor puede estar vinculado a múltiples empresas.
* [ ] Existe `VinculacionConductorEmpresa`.
* [ ] No se duplica el conductor por empresa.
* [ ] `ConductorId + EmpresaId` es único.
* [ ] El estado de la vinculación puede cambiar sin eliminar historial.

---

# 11. Vehículos y unidades

## Vehículo

* [ ] Vehículo pertenece exclusivamente a un conductor.
* [ ] Placa es única.
* [ ] Capacidad está almacenada.
* [ ] Vehículo puede desactivarse.
* [ ] No se reasigna un vehículo a otro conductor.

## UnidadOperativa

* [ ] Unidad referencia conductor.
* [ ] Unidad referencia vehículo.
* [ ] Vehículo no pertenece a más de una unidad.
* [ ] Conductor de Unidad coincide con conductor de Vehículo.
* [ ] La consistencia se valida en backend.
* [ ] La unidad se asigna operacionalmente a un Servicio (no a una Jornada).
* [ ] Al desactivar un vehículo, su `UnidadOperativa` asociada también se desactiva.
* [ ] Al cambiar de vehículo, se crea una nueva `UnidadOperativa` en vez de reutilizar la anterior.
* [ ] Los Servicios históricos no se modifican al cambiar de unidad operativa.

---

# 12. Importación Excel

* [ ] Cada importación queda registrada.
* [ ] Se identifica la empresa.
* [ ] Se identifica el coordinador que realizó la importación.
* [ ] Se registra nombre del archivo.
* [ ] Se registra fecha de importación.
* [ ] Se registra estado.
* [ ] No se crean usuarios duplicados por importar nuevamente la misma cédula.
* [ ] Se detectan registros nuevos.
* [ ] Se detectan cambios.
* [ ] Si la cédula ya existe en otra empresa, se actualizan automáticamente `Empleado.EmpresaId` y `UsuarioRol(EMPLEADO).EmpresaId` en la misma transacción.
* [ ] El cambio de empresa detectado por importación no modifica `ProgramacionTransporte`, `ServicioPasajero`, `Jornada` ni `Servicio` históricos.
* [ ] Los cambios no sobrescriben información automáticamente sin la regla correspondiente.
* [ ] No se eliminan programaciones anteriores de forma indiscriminada.
* [ ] `ImportacionExcel.Estado` utiliza únicamente: `PENDIENTE`, `PROCESANDO`, `COMPLETADA`, `COMPLETADA_CON_ADVERTENCIAS`, `ERROR`.
* [ ] El formato definitivo del Excel no se inventó.
* [ ] Si el formato real todavía no está disponible, la implementación queda marcada como pendiente.

---

# 13. Programación de transporte

* [ ] Existe `ProgramacionTransporte`.
* [ ] Tiene EmpresaId.
* [ ] Tiene EmpleadoId.
* [ ] Tiene SedeId.
* [ ] Tiene Fecha.
* [ ] Tiene Hora.
* [ ] Tiene Tipo.
* [ ] Tiene DirecciónRecogida.
* [ ] Tiene BarrioRecogida.
* [ ] Fecha y hora permanecen separadas.
* [ ] Entrada representa empleado → sede.
* [ ] Salida representa sede → empleado.
* [ ] Una programación puede permanecer temporalmente sin servicio.
* [ ] Una programación no puede tener más de un ServicioPasajero.

---

# 14. Jornada

* [ ] Jornada pertenece a una empresa.
* [ ] Jornada NO tiene `UnidadOperativaId` propio (la unidad se asigna por Servicio, no por Jornada).
* [ ] Una misma Jornada puede tener Servicios asignados a distintas UnidadesOperativas.
* [ ] Tiene FechaOperativa.
* [ ] No contiene HoraInicio.
* [ ] No contiene HoraFin.
* [ ] No existe restricción única innecesaria Empresa + FechaOperativa.
* [ ] Se permite representar servicios alrededor de medianoche.
* [ ] FechaOperativa representa el bloque operativo y no necesariamente la fecha del primer servicio.

---

# 15. Servicio

* [ ] Servicio pertenece a Jornada.
* [ ] Servicio pertenece a una Sede.
* [ ] Servicio tiene Fecha.
* [ ] Servicio tiene HoraProgramada.
* [ ] Servicio tiene Tipo.
* [ ] Servicio tiene Estado.
* [ ] Servicio tiene HoraInicioReal.
* [ ] Servicio tiene HoraFinReal.
* [ ] Servicio no duplica EmpresaId.
* [ ] Servicio SÍ tiene `UnidadOperativaId` propio (opcional/nulo hasta asignarse), no heredado de la Jornada.
* [ ] La empresa se obtiene mediante Jornada.
* [ ] La unidad se obtiene directamente de `Servicio.UnidadOperativaId`, sin pasar por Jornada.
* [ ] Reasignar la unidad operativa de un Servicio no modifica su `JornadaId`.
* [ ] Una misma UnidadOperativa puede tener Servicios en distintas Jornadas y de distintas fechas.
* [ ] Se permiten servicios de fechas diferentes a FechaOperativa.
* [ ] Estados implementados correctamente.
* [ ] No se publica un servicio sin unidad cuando la regla exige asignación.

---

# 16. ServicioPasajero

* [ ] ServicioPasajero referencia Servicio.
* [ ] ServicioPasajero referencia ProgramacionTransporte.
* [ ] ServicioPasajero referencia Empleado.
* [ ] Guarda estado actual.
* [ ] Guarda orden.
* [ ] Guarda dirección histórica del servicio.
* [ ] Guarda coordenadas cuando existen.
* [ ] ProgramacionTransporte no puede estar asignada a dos pasajeros operativos.
* [ ] No existe historial de estados innecesario.
* [ ] El orden puede ser modificado por el conductor.

---

# 17. Estados de pasajero

Verificar que existan:

* [ ] `PROGRAMADO`
* [ ] `CONFIRMADO`
* [ ] `NO_ASISTIRA`
* [ ] `CONDUCTOR_LLEGO`
* [ ] `ESPERANDO`
* [ ] `NO_CONTESTA`
* [ ] `NO_SE_ENCUENTRA`
* [ ] `DIRECCION_INCORRECTA`
* [ ] `NO_SE_PUDO_RECOGER`
* [ ] `RECOGIDO`
* [ ] `EN_VEHICULO`
* [ ] `DEJADO_EN_DESTINO`
* [ ] `CANCELADO`

---

# 18. Ejecución de ruta

* [ ] El conductor inicia la ruta manualmente.
* [ ] Se registra hora real de inicio.
* [ ] La llegada puede detectarse mediante geolocalización.
* [ ] Existe alternativa manual.
* [ ] El tiempo de espera máximo es 2 minutos.
* [ ] El conductor puede llamar al empleado.
* [ ] No se almacenan grabaciones de llamadas.
* [ ] El empleado puede compartir ubicación.
* [ ] El conductor puede abrir navegación.
* [ ] Se registra hora de finalización.
* [ ] Se registra ubicación final cuando corresponda.
* [ ] No se permite finalizar una entrada con pasajeros pendientes incompatibles con la finalización.
* [ ] Las salidas pueden finalizar donde corresponda operacionalmente.

---

# 19. Planificación asistida

* [ ] Se considera capacidad.
* [ ] Se considera horario.
* [ ] Se consideran ubicaciones.
* [ ] Se considera sede.
* [ ] Se considera continuidad geográfica.
* [ ] Se considera tiempo estimado de viaje.
* [ ] Se considera un tiempo conjunto aproximado de recogida, implementado como parámetro configurable (referencia inicial: 25 minutos, no definitivo).
* [ ] Se considera llegada a sede al menos 15 minutos antes.
* [ ] Se considera el punto final del servicio anterior.
* [ ] Se considera la primera recogida del servicio siguiente.
* [ ] Se utiliza la heurística de pasajeros alejados primero cuando corresponda.
* [ ] El algoritmo genera propuestas.
* [ ] El algoritmo no modifica automáticamente la planificación definitiva.
* [ ] El coordinador puede aceptar.
* [ ] El coordinador puede rechazar.
* [ ] El coordinador puede modificar.
* [ ] No se presenta la recomendación algorítmica como una obligación.
* [ ] No existe una fórmula inventada si todavía está pendiente en el SDD.

---

# 20. Asignación manual

* [ ] El coordinador puede redistribuir servicios.
* [ ] El sistema no bloquea por distancia.
* [ ] El sistema no bloquea por carga de trabajo como regla dura.
* [ ] El sistema sí detecta incompatibilidades temporales.
* [ ] Para reasignar una unidad se puede retirar primero de otro servicio.
* [ ] El servicio puede quedar temporalmente sin unidad.
* [ ] Un servicio sin unidad no puede publicarse.

---

# 21. Publicación

* [ ] Existe operación equivalente a `Enviar`.
* [ ] Publicar cambia el estado correctamente.
* [ ] Se notifica al conductor.
* [ ] Se notifica al empleado correspondiente.
* [ ] Se notifican solamente usuarios afectados por modificaciones.
* [ ] Una modificación posterior a publicación sigue las reglas definidas.
* [ ] No se publica una Jornada con servicios irresueltos.
* [ ] No se publica un servicio vacío que deba eliminarse/reasignarse.

---

# 22. Incidencias

* [ ] Existe `Incidencia`.
* [ ] Está relacionada con ServicioPasajero.
* [ ] Tiene tipo.
* [ ] Tiene descripción.
* [ ] Tiene fecha/hora.
* [ ] Puede almacenar coordenadas.
* [ ] Se pueden registrar:

  * [ ] NO_CONTESTA
  * [ ] NO_SE_ENCUENTRA
  * [ ] DIRECCION_INCORRECTA
  * [ ] NO_SE_PUDO_RECOGER
  * [ ] UBICACION_MODIFICADA
  * [ ] OTRA

---

# 23. Evidencias

* [ ] Existe `Evidencia`.
* [ ] Está asociada a Incidencia.
* [ ] El único tipo de evidencia implementado es `FOTOGRAFIA`.
* [ ] Guarda tipo.
* [ ] Guarda referencia al archivo.
* [ ] Guarda fecha/hora.
* [ ] PostgreSQL no almacena innecesariamente el binario de la fotografía.
* [ ] El mecanismo definitivo de almacenamiento externo está documentado si aún está pendiente.

---

# 24. Chat

* [ ] Existe `Conversacion`.
* [ ] Cada ServicioPasajero puede tener máximo una conversación.
* [ ] La conversación es individual.
* [ ] Participan conductor y empleado.
* [ ] Existe `Mensaje`.
* [ ] Cada mensaje identifica Usuario.
* [ ] Se registra fecha/hora.
* [ ] No existe chat grupal.

---

# 25. Notificaciones

* [ ] Existe `Notificacion`.
* [ ] Está asociada a Usuario.
* [ ] Tiene título.
* [ ] Tiene mensaje.
* [ ] Tiene tipo.
* [ ] Tiene estado leído/no leído.
* [ ] Tiene fecha/hora.
* [ ] Se pueden marcar como leídas.

---

# 26. Alerta de ruta no iniciada

* [ ] Se evalúan servicios de entrada.
* [ ] La evaluación ocurre aproximadamente una hora antes.
* [ ] Se comprueba si la ruta fue iniciada.
* [ ] La alerta llega al conductor responsable.
* [ ] No se genera innecesariamente para servicios ya iniciados.
* [ ] No se confunde esta alerta con la futura hora recomendada de inicio.
* [ ] No existe una fórmula inventada para la hora recomendada.

---

# 27. Autenticación y seguridad

* [ ] Login mediante cédula y contraseña.
* [ ] Password almacenada mediante hash seguro.
* [ ] Nunca se almacena contraseña en texto plano.
* [ ] JWT correctamente firmado.
* [ ] JWT contiene únicamente información necesaria.
* [ ] Endpoints protegidos correctamente.
* [ ] Roles verificados en backend.
* [ ] Empresa verificada en backend.
* [ ] No se confía en claims manipulables por el cliente.
* [ ] No existen endpoints que permitan saltarse autorización mediante IDs.
* [ ] Cuentas inactivas no pueden autenticarse.
* [ ] Cuenta pendiente de activación puede completar el flujo correspondiente.
* [ ] Los tokens de verificación (`TokenVerificacion`, confirmación de correo y restablecimiento de contraseña) se almacenan mediante hash, nunca en texto plano (sustituye al antiguo `CodigoActivacion`).
* [ ] Un conductor solo puede generar un código para un empleado asignado a alguno de sus Servicios (mediante `ServicioPasajero` → `Servicio.UnidadOperativaId` → Conductor).
* [ ] Un conductor no puede generar códigos para empleados no vinculados a ninguno de sus servicios.
* [ ] El código es de un solo uso: se invalida (`Utilizado = true`) tras activarse correctamente.
* [ ] El código respeta expiración y límite de intentos fallidos.
* [ ] El coordinador y el `ADMINISTRADOR_PLATAFORMA` no participan en ningún paso de la generación ni de la activación.
* [ ] No se implementó envío por WhatsApp, SMS, email ni proveedor externo.

---

# 28. Históricos

* [ ] No se eliminan físicamente entidades necesarias para histórico.
* [ ] Programaciones antiguas mantienen su empresa original.
* [ ] Servicios antiguos mantienen su contexto original.
* [ ] Cambiar la empresa actual del empleado no modifica históricos.
* [ ] Cambiar datos actuales del empleado no modifica direcciones históricas de servicios.
* [ ] Evidencias históricas permanecen referenciadas.

---

# 29. Base de datos

* [ ] PostgreSQL configurado.
* [ ] EF Core configurado.
* [ ] Migraciones reproducibles.
* [ ] Claves primarias correctas.
* [ ] Claves foráneas correctas.
* [ ] Restricciones únicas correctas.
* [ ] Campos obligatorios definidos correctamente.
* [ ] Campos opcionales permiten `NULL`.
* [ ] Fechas y horas utilizan tipos adecuados.
* [ ] Zona horaria está definida/documentada.
* [ ] No existen tablas innecesarias.
* [ ] No existen columnas que contradigan `data-model.md`.
* [ ] Índices importantes están presentes.
* [ ] No existen índices redundantes sin justificación.

---

# 30. API

* [ ] Todos los endpoints utilizan DTOs.
* [ ] No se exponen entidades EF directamente.
* [ ] Validaciones de entrada implementadas.
* [ ] Respuestas HTTP coherentes.
* [ ] Errores controlados.
* [ ] Swagger/OpenAPI disponible.
* [ ] Endpoints protegidos según rol.
* [ ] Endpoints protegidos según empresa.
* [ ] No existen endpoints administrativos sin autorización.

---

# 31. Pruebas unitarias

* [ ] Usuario.
* [ ] Roles.
* [ ] Empresa.
* [ ] Sede.
* [ ] Empleado.
* [ ] Conductor.
* [ ] Vehículo.
* [ ] UnidadOperativa.
* [ ] ProgramacionTransporte.
* [ ] Jornada.
* [ ] Servicio.
* [ ] ServicioPasajero.
* [ ] Estados.
* [ ] Incidencias.
* [ ] Reglas de planificación.
* [ ] Capacidad.
* [ ] Conflictos temporales.
* [ ] Seguridad multiempresa.

---

# 32. Pruebas de integración

* [ ] Base PostgreSQL de pruebas reproducible.
* [ ] Migraciones probadas.
* [ ] Registro/login probado.
* [ ] JWT probado.
* [ ] Autorización probada.
* [ ] Multiempresa probado.
* [ ] Importación probada.
* [ ] Programación probada.
* [ ] Jornada probada.
* [ ] Servicio probado.
* [ ] Pasajeros probados.
* [ ] Publicación probada.
* [ ] Ejecución probada.
* [ ] Flujo completo probado.

---

# 33. Docker

* [ ] API puede ejecutarse mediante Docker.
* [ ] PostgreSQL puede ejecutarse mediante Docker.
* [ ] Variables de entorno configuradas.
* [ ] Secretos no están en Git.
* [ ] Contenedores pueden iniciarse desde una configuración limpia.
* [ ] Migraciones pueden ejecutarse correctamente.
* [ ] Aplicación puede comunicarse con PostgreSQL dentro del entorno Docker.

---

# 34. Configuración

* [ ] Connection string fuera del código.
* [ ] JWT secret fuera del código.
* [ ] Configuración por entorno.
* [ ] Desarrollo separado de producción.
* [ ] No existen contraseñas reales en repositorio.
* [ ] No existen tokens reales en repositorio.
* [ ] No existen claves privadas en repositorio.

---

# 35. Documentación

* [ ] `CONSTITUTION.md` actualizado.
* [ ] `AGENTS.md` actualizado.
* [ ] `spec.md` actualizado.
* [ ] `data-model.md` actualizado.
* [ ] `plan.md` actualizado.
* [ ] `tasks.md` actualizado.
* [ ] Este `checklist.md` actualizado.
* [ ] Decisiones pendientes documentadas.
* [ ] Cambios arquitectónicos documentados.
* [ ] API documentada.
* [ ] Instrucciones de ejecución documentadas.

---

# 36. Control contra sobreingeniería

* [ ] No se añadieron microservicios innecesarios.
* [ ] No se añadió un message broker sin requisito.
* [ ] No se añadió CQRS sin necesidad.
* [ ] No se añadió Event Sourcing.
* [ ] No se añadieron patrones complejos sin justificación.
* [ ] No se añadieron entidades únicamente para “preparar el futuro”.
* [ ] No se añadieron campos no requeridos.
* [ ] No se duplicó información sin necesidad.
* [ ] No se creó infraestructura innecesaria.
* [ ] La solución mantiene el enfoque de monolito modular.

---

# 37. Control contra decisiones inventadas

Antes de marcar el proyecto como terminado:

* [ ] Claude no inventó el formato definitivo del Excel.
* [ ] El mecanismo de activación implementado es exclusivamente el código generado por el conductor (ver `data-model.md` §28); no se añadió WhatsApp, SMS, email ni proveedor externo, ni ninguna verificación de identidad adicional.
* [ ] Claude no inventó un proveedor definitivo de mapas sin decisión.
* [ ] Claude no inventó un radio definitivo de geocerca.
* [ ] Claude no inventó la fórmula definitiva del algoritmo.
* [ ] Claude no inventó pesos definitivos para la planificación.
* [ ] Claude no inventó una estrategia definitiva de selección de rol si todavía está pendiente.
* [ ] Claude no inventó refresh tokens si aún no se ha decidido.
* [ ] Claude no inventó almacenamiento definitivo de evidencias.
* [ ] Claude no inventó reglas de edición post-publicación.
* [ ] Toda decisión nueva está documentada y justificada.

---

# 38. Revisión de código

* [ ] La solución compila sin errores.
* [ ] No existen warnings críticos ignorados.
* [ ] No existen métodos sin implementación utilizados en producción.
* [ ] No existen `TODO` críticos.
* [ ] No existen excepciones silenciosamente ignoradas.
* [ ] No existen credenciales en código.
* [ ] No existen logs con contraseñas.
* [ ] No existen logs con tokens JWT.
* [ ] No existen datos sensibles innecesarios en logs.
* [ ] No existen consultas claramente N+1 en operaciones críticas.
* [ ] No existen consultas sin filtros de empresa donde sean necesarios.
* [ ] No existen endpoints que permitan modificar datos sin autorización.

---

# 39. Prueba de seguridad multiempresa

Crear como mínimo dos empresas:

* Empresa A
* Empresa B

Crear usuarios y datos para ambas.

Verificar:

* [ ] Coordinador A puede consultar Empresa A.
* [ ] Coordinador A puede modificar Empresa A.
* [ ] Coordinador A no puede consultar Empresa B.
* [ ] Coordinador A no puede modificar Empresa B.
* [ ] Empleado A no puede consultar Empleado B.
* [ ] Conductor A no puede consultar servicios de Empresa B salvo que esté autorizado por vinculación.
* [ ] Un cambio manual de IDs no permite saltarse la seguridad.
* [ ] La API rechaza operaciones fuera del contexto autorizado.

---

# 40. Prueba de múltiples roles

Crear un Usuario con más de un rol.

Verificar:

* [ ] El usuario conserva una única identidad.
* [ ] No existen usuarios duplicados.
* [ ] Los roles aparecen correctamente.
* [ ] Cada operación respeta los permisos correspondientes.
* [ ] El contexto de empresa se respeta.
* [ ] El usuario no puede elevar privilegios por modificar la petición.
* [ ] El mecanismo técnico para seleccionar/representar el rol activo está documentado.

---

# 41. Flujo funcional completo

Ejecutar una prueba desde cero:

* [ ] Crear Empresa.
* [ ] Crear primer Coordinador.
* [ ] Coordinador inicia sesión.
* [ ] Crear Sede.
* [ ] Importar empleados.
* [ ] Crear usuarios de empleados.
* [ ] Activar cuenta de empleado.
* [ ] Crear conductor.
* [ ] Vincular conductor a empresa.
* [ ] Crear vehículo.
* [ ] Crear unidad operativa.
* [ ] Crear programación.
* [ ] Crear Jornada.
* [ ] Crear Servicio.
* [ ] Asignar unidad.
* [ ] Asignar pasajeros.
* [ ] Publicar.
* [ ] Empleado confirma.
* [ ] Conductor inicia ruta.
* [ ] Conductor llega.
* [ ] Conductor registra estados.
* [ ] Conductor finaliza.
* [ ] Registrar incidencia cuando corresponda.
* [ ] Registrar evidencia cuando corresponda.
* [ ] Consultar histórico.

---

# 42. Definición de terminado global

El proyecto solamente puede considerarse terminado cuando:

* [ ] Todas las tareas obligatorias de `tasks.md` están completadas.
* [ ] Todas las verificaciones críticas de este checklist están cumplidas.
* [ ] La solución compila.
* [ ] Las pruebas unitarias pasan.
* [ ] Las pruebas de integración pasan.
* [ ] Las migraciones funcionan desde una base limpia.
* [ ] Docker funciona.
* [ ] Swagger funciona.
* [ ] La autenticación funciona.
* [ ] La autorización funciona.
* [ ] El aislamiento multiempresa funciona.
* [ ] El modelo de datos coincide con `data-model.md`.
* [ ] La implementación coincide con `spec.md`.
* [ ] La arquitectura coincide con `plan.md`.
* [ ] Se cumplen las reglas de `CONSTITUTION.md`.
* [ ] Se cumplen las instrucciones de `AGENTS.md`.
* [ ] No existen decisiones arquitectónicas importantes sin documentar.
* [ ] No existen requisitos inventados.
* [ ] No existen vulnerabilidades críticas conocidas.
* [ ] No existen secretos en el repositorio.
* [ ] Ninguna tarea funcional fue marcada como completada basándose únicamente en que el código compilaba.

---

# 43. Revisión final obligatoria

Antes de declarar `DONE`, Claude Code debe ejecutar una revisión final:

1. Leer nuevamente `CONSTITUTION.md`.
2. Leer nuevamente `AGENTS.md`.
3. Revisar `spec.md`.
4. Revisar `data-model.md`.
5. Revisar `plan.md`.
6. Revisar `tasks.md`.
7. Ejecutar este checklist.
8. Identificar incumplimientos.
9. Corregir los incumplimientos que estén dentro del alcance.
10. Documentar los pendientes que dependan de decisiones todavía no tomadas.

La salida final debe indicar:

* tareas completadas;
* tareas pendientes;
* checklist cumplido;
* checklist incumplido;
* decisiones pendientes;
* pruebas ejecutadas;
* resultado de compilación;
* resultado de integración;
* riesgos conocidos.

No declarar el proyecto terminado si existen incumplimientos críticos.
