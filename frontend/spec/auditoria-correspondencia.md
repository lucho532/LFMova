# Auditoría de correspondencia — Especificación Frontend vs. Swagger real vs. Constitución

Fecha: 2026-09-18

**Actualización 2026-09-18:** se resolvió el bloqueante más urgente de la sección 5 (punto 1). Se agregaron dos endpoints nuevos de descubrimiento:
- `GET /api/conductores/mis-servicios` — devuelve los servicios (`ServicioDto`, ahora con `EmpresaId`) asignados a las unidades operativas del conductor autenticado, resuelto internamente desde el usuario del token (sin necesitar `conductorId` de antemano).
- `GET /api/empleados/mis-servicios` — devuelve los servicios (`ServicioDelEmpleadoDto`, nuevo) del empleado autenticado como pasajero, resuelto internamente desde el usuario del token.

Ambos están cubiertos por pruebas unitarias (278 unit + 10 integration, todas en verde) y probados con `curl` contra la API real. Esto desbloquea la construcción de "Mi jornada"/"Mis servicios" para CONDUCTOR y EMPLEADO (secciones 2.3/2.4 y punto 1 de la sección 3), aunque el resto de limitaciones de esas dos tablas (temporizador, checklist de finalización, subida de evidencia) siguen pendientes.
Fuentes utilizadas:
- `frontend/spec/especificación_Frontend` (documento a auditar)
- `GET /swagger/v1/swagger.json` de la API real en ejecución (`http://localhost:5109`), leído endpoint por endpoint y esquema por esquema
- Código fuente real de los 15 controladores en `src/LFMova.Api/Controllers/`
- `AGENTS.md` (Constitución) y `docs/sdd/03-architecture/plan.md` §57 (arquitectura del frontend ya decidida)

Regla aplicada (spec §59 y `plan.md` §57.6): si el backend no expone un endpoint/DTO/propiedad, esa parte del frontend **no se construye todavía**; se documenta aquí y se deja pendiente de decisión.

---

## 1. Resumen ejecutivo

La especificación describe una aplicación completa para los 4 roles. El backend real ya implementa **toda la operación de ADMINISTRADOR_PLATAFORMA** y **la gran mayoría de la gestión administrativa del COORDINADOR** (empleados, conductores, sedes, programaciones, jornadas, servicios, asignación, pasajeros, chat, incidencias, evidencias, notificaciones). Sin embargo, hay **cuatro pantallas/funciones que la especificación pide y que el backend no soporta en absoluto todavía**, y varias más que solo se pueden construir de forma parcial o distinta a como las dibuja el documento.

No se debe implementar código de frontend para las partes marcadas ❌ sin antes tomar una decisión (ver §5).

---

## 2. Correspondencia por rol

### 2.1 ADMINISTRADOR_PLATAFORMA

| Spec pide (§12-15, §64) | Backend real | Veredicto |
|---|---|---|
| Login | `POST /api/autenticacion/iniciar-sesion` | ✅ Ya implementado en el frontend actual |
| Dashboard con indicadores | No existe ningún endpoint de estadísticas/dashboard | ❌ Sin soporte. No inventar contadores; a lo sumo, derivar "Empresas totales/activas" del propio `GET /api/empresas` ya cargado (no es un endpoint nuevo, es cálculo trivial sobre datos reales) |
| Empresas (listar, buscar, ver/editar) | `GET /api/empresas` (sin parámros de búsqueda/filtro/paginación) | ⚠️ Parcial: listar ✅ ya implementado; "Buscar empresa..." no tiene soporte de backend → si se hace, debe ser filtro **client-side** sobre la lista ya cargada, nunca un parámetro de API inventado |
| Crear empresa (+ primer coordinador) | `POST /api/empresas` (`CrearEmpresaDto`: Nombre, Cif, Direccion, CedulaCoordinador, PasswordCoordinador) | ✅ Ya implementado |
| Detalle empresa + activar/desactivar empresa y coordinadores | `GET /api/empresas/{id}`, `.../activar`, `.../desactivar`, `GET/POST/DELETE .../coordinadores` | ✅ Ya implementado |
| Configuración global | **No existe ningún endpoint, entidad ni servicio de "configuración global"** en todo el backend | ❌ Sin soporte. No crear esta pantalla ni el ítem de menú hasta que exista una especificación de backend concreta (spec §59) |
| Mi cuenta | **No existe ningún endpoint de perfil propio** (`/me`, `PerfilDto`, cambio de contraseña, etc.) para ningún rol | ❌ Sin soporte para ningún rol. No implementar |
| Cerrar sesión | Cliente-side (borrar token) | ✅ Ya implementado |

**Conclusión:** el menú del administrador definido en spec §12.1 debe reducirse, por ahora, a `Empresas` (ya construido) + `Cerrar sesión`. `Inicio` (dashboard), `Configuración global` y `Mi cuenta` no tienen backend.

---

### 2.2 COORDINADOR

| Spec pide | Backend real | Veredicto |
|---|---|---|
| Dashboard operativo (§18) | Sin endpoint de resumen/dashboard | ❌ Sin soporte como "dashboard" dedicado. Se podría montar un panel de inicio que simplemente enlace a las secciones reales, sin inventar contadores que la API no entregue |
| Empleados: lista con Nombre/Cédula/**Sede**/**Horario**/Estado (§19) | `GET /api/empresas/{id}/empleados` → `EmpleadoDto { empleadoId, usuarioId, empresaId, cedula, nombreCompleto, telefono, direccion, barrio, activo }` | ⚠️ **Mismatch de datos**: `Empleado` no tiene `Sede` ni `Horario` como atributos propios (correcto según el modelo: eso varía por `ProgramacionTransporte`, no es fijo por empleado — ver `AGENTS.md` §28). La tabla debe mostrar Nombre/Cédula/Teléfono/Barrio/Estado, **no** Sede/Horario tal como los dibuja la maqueta |
| Empleados: Consultar/Actualizar/Activar-Desactivar (§19) | `GET`, `PUT`, `.../activar`, `.../desactivar` | ✅ Soportado (`ActualizarEmpleadoDto` — falta confirmar sus campos exactos antes de construir el formulario de edición) |
| Ficha del empleado: Servicios y Programaciones (§20) | No existe ningún endpoint "programaciones de un empleado" ni "servicios de un empleado". `ProgramacionesController` solo lista **todas** las programaciones de la empresa (sin filtro por empleado); no hay endpoint para listar `ServicioPasajero` por empleado | ❌ Sin soporte directo. Alternativa no inventada: filtrar client-side el resultado de `GET /api/empresas/{id}/programaciones` por `empleadoId` (dato real, sin inventar endpoint) — aceptable pero ineficiente si hay muchos registros; documentarlo como limitación conocida |
| Conductores: lista y ficha (§21) | `GET /api/empresas/{id}/conductores`, detalle, vehículos (`api/conductores/{id}/vehiculos`), unidades operativas | ✅ Soportado. Nota: la API oculta intencionalmente con qué otras empresas está vinculado un conductor compartido (aislamiento multiempresa) — el frontend no debe intentar mostrar esa información aunque exista el campo `empresaIdsVinculadosActivos`, ya el backend ya lo filtra |
| Sedes: lista y formulario (§22) | `SedeDto { sedeId, empresaId, nombre, direccion, ciudad, barrio, latitud, longitud, activa }` | ✅ Coincide exactamente con los campos de la maqueta |
| Importación Excel (§23) | **No existe ningún controlador, servicio ni endpoint de importación.** Solo existe la entidad `ImportacionExcel` en Domain/Infrastructure (tabla y configuración EF), sin capa de Application ni Api | ❌ Sin soporte end-to-end. No construir esta pantalla |
| Programaciones (§24) | `GET/POST/PUT /api/empresas/{id}/programaciones` (`ProgramacionDto`: empleadoId, sedeId, fecha, hora, tipo, direccionRecogida, barrioRecogida) | ✅ Soportado, sin búsqueda/filtro de servidor (client-side si se necesita) |
| Jornadas (§25) | `GET/POST /api/empresas/{id}/jornadas`, `.../publicar`. `JornadaDto` = `{ jornadaId, empresaId, fechaOperativa }` **solamente** | ⚠️ **Mismatch**: la jornada NO tiene "Unidad operativa", "Conductor" ni "Estado" propios (confirmado por `AGENTS.md` §19/§21: eso vive en cada `Servicio`, una jornada puede tener varios servicios con distintas unidades). La pantalla de jornada debe mostrar la fecha operativa y la lista de sus servicios (cada uno con su propia unidad/conductor/estado vía `GET .../servicios`), no un único "conductor"/"estado" a nivel de jornada como sugiere el diagrama del §25 |
| Servicios: lista Hora/Tipo/Sede/**Pasajeros**/Estado (§26) | `ServicioDto { servicioId, jornadaId, unidadOperativaId, sedeId, fecha, horaProgramada, tipo, estado, horaInicioReal, horaFinReal }` — **no incluye conteo de pasajeros** | ⚠️ Parcial: el conteo de pasajeros no viene en el DTO de lista; para mostrarlo habría que llamar `GET .../pasajeros` por cada servicio (N+1), lo cual es real pero costoso. Alternativa: omitir la columna "Pasajeros" en la lista y mostrarla solo en el detalle |
| Detalle de servicio con Conductor/Vehículo (§26) | `ServicioDto` solo trae `unidadOperativaId` (no nombre de conductor ni placa) | ⚠️ Requiere una segunda consulta (`GET /api/conductores/{id}/unidades-operativas` o similar) para resolver el nombre — el backend no entrega esos datos "enriquecidos" en un único DTO. Documentar como limitación, no inventar un campo |
| Asignaciones / Planificación (§27-28) | `GET .../propuesta-planificacion` (`PropuestaPlanificacionDto`), `POST .../asignar-unidad`, `POST .../retirar-unidad`, `PUT .../pasajeros/{id}/orden` | ✅ Soportado. El backend ya modela exactamente el flujo "propuesta del sistema → coordinador decide" que pide §27; el frontend debe rotular la propuesta como sugerencia, nunca como definitiva, hasta que el coordinador ejecute `asignar-unidad`/`orden` |
| Publicaciones (§29) | **Publicar existe a nivel de Jornada** (`POST .../jornadas/{id}/publicar`), no a nivel de Servicio individual como dibuja la maqueta ("Servicio → Estado: Pendiente de publicar → [Publicar]") | ⚠️ **Mismatch de granularidad**: la acción real publica toda la jornada de una vez, no un servicio suelto. La pantalla debe reflejar esto (botón "Publicar jornada", no "Publicar" por cada servicio) |
| Incidencias (§38, vista de coordinador) | El único endpoint de incidencias (`IncidenciasController`) está restringido **exclusivamente al conductor asignado al servicio** (`EsConductorAsignadoAsync`) — un coordinador no puede leerlas ni crearlas | ❌ Sin soporte para una pantalla de "Incidencias" del coordinador. Esta función es del CONDUCTOR, no del COORDINADOR, contradiciendo la ubicación implícita de la pantalla en la lista de "Pantallas mínimas" del coordinador (§64) |
| Mi cuenta | Sin backend (ver 2.1) | ❌ Sin soporte |

---

### 2.3 CONDUCTOR

| Spec pide (§30-39, §64) | Backend real | Veredicto |
|---|---|---|
| Login | ✅ | ✅ |
| Mi jornada / Dashboard "próximo servicio" (§31) | **No existe ningún endpoint que liste los servicios/jornadas asignados al conductor autenticado.** `ConductorController` solo expone `GET /{conductorId}` (su propio perfil) y activar/desactivar. `JornadasController` y `ServiciosController` exigen rol `COORDINADOR` de la empresa — un conductor nunca pasa esa comprobación | ❌ **Bloqueante crítico**: sin un endpoint de descubrimiento ("mis servicios de hoy"), el conductor no tiene forma de saber qué `empresaId`/`jornadaId`/`servicioId` debe usar para llamar a `iniciar`, `finalizar`, `marcar-llegada`, etc. Todas esas acciones existen y funcionan, pero **solo si el frontend ya conoce el ID del servicio** |
| Mis servicios (§32) | Mismo problema que arriba | ❌ Sin endpoint de listado |
| Servicio del conductor: iniciar, finalizar, marcar llegada/esperando, cambiar estado pasajero (§32-33) | `POST .../servicios/{id}/iniciar`, `.../finalizar`, `.../pasajeros/{id}/marcar-llegada`, `.../marcar-esperando`, `.../estado` | ✅ Soportado — **una vez que se conoce el ID del servicio** (ver bloqueante anterior) |
| Pasajeros del servicio (§36) | `GET .../servicios/{id}/pasajeros` (incluye nombre y teléfono, no expone más datos de otros pasajeros) | ✅ Soportado, y ya respeta la restricción de "solo lo necesario" del §36 |
| Llamar / Chat individual (§32, §37) | Llamar: no es un endpoint (se implementa con `tel:` en el propio dispositivo, no requiere backend). Chat: `GET/POST .../pasajeros/{id}/mensajes` | ✅ Soportado |
| Temporizador de llegada (§34) | No hay endpoint de temporización/cuenta regresiva en tiempo real | ❌ Sin soporte. No implementar un temporizador cliente que "invente" un estado — coincide con lo que la propia spec §34 ya advierte |
| Finalización con condiciones (§35) | El backend no expone explícitamente qué condiciones faltan (no hay un DTO tipo "requisitos de finalización"); `FinalizarAsync` simplemente acepta o rechaza con `InvalidOperationException` y un mensaje | ⚠️ Parcial: se puede mostrar el mensaje de error real cuando el backend rechace la finalización, pero no se puede pre-calcular una lista de checks (✓/○) porque el backend no la entrega. No inventar esa lista |
| Incidencias + Evidencias (§38-39) | `POST/GET .../incidencias`, `POST .../incidencias/{id}/evidencias` (`AgregarEvidenciaDto`: referencia de archivo, no sube el binario) | ✅ Soportado. La subida real del archivo a un storage no está definida — el DTO espera una referencia, no el binario, así que "Tomar fotografía"/"Seleccionar fotografía" tendría que resolver primero **dónde se sube el archivo**, lo cual no está definido (posible bloqueante para esa pantalla concreta) |
| Ubicación del conductor | No hay endpoint para que el conductor comparta su propia ubicación (solo existe `CompartirUbicacionAsync` para el **empleado**, ruta `.../ubicacion`) | ❌ Sin soporte de ubicación del conductor |
| Mi cuenta | Sin backend | ❌ Sin soporte |

**Conclusión:** el rol CONDUCTOR es el más comprometido. La acción crítica que falta es un endpoint de "mis servicios/mi jornada de hoy"; sin él, **ninguna pantalla operativa del conductor es funcionalmente utilizable** más allá de activar/desactivar su propio perfil.

---

### 2.4 EMPLEADO

| Spec pide (§40-44, §64) | Backend real | Veredicto |
|---|---|---|
| Login | ✅ | ✅ |
| Mi transporte / Dashboard (§41) | Ningún endpoint devuelve "el próximo servicio del empleado autenticado" | ❌ Sin soporte |
| Mis servicios, próximos e históricos (§42) | No existe ningún endpoint "servicios de un empleado". `ServiciosPasajeroController` está anidado bajo `empresas/{id}/jornadas/{id}/servicios/{id}/pasajeros` y exige conocer esos IDs de antemano; no hay una vista "mía" transversal | ❌ Sin soporte |
| Detalle del servicio del empleado (§43) | Mismo bloqueo: sin saber el `servicioId`, no se puede llegar a ver el detalle | ❌ Sin soporte |
| Compartir ubicación | `PUT .../pasajeros/{id}/ubicacion` (solo el propio empleado dueño del pasajero) | ✅ Soportado, pero solo alcanzable si ya se conoce el `servicioPasajeroId` |
| Chat individual (§37) | `GET/POST .../pasajeros/{id}/mensajes` | ✅ Soportado, mismo bloqueo de descubrimiento |
| Mi cuenta | Sin backend | ❌ Sin soporte |

**Conclusión:** el rol EMPLEADO tiene exactamente el mismo bloqueante que CONDUCTOR: no existe ningún endpoint de "descubrimiento" (listar mis propios servicios/pasajero). Todo lo demás que sí existe (compartir ubicación, chat) requiere IDs que hoy no hay forma de obtener desde el propio rol.

---

## 3. Elementos de la especificación que, si se implementan literalmente, violarían la regla "no inventar" (spec §59 / `plan.md` §57.6)

1. **Configuración global** (admin) — no existe backend.
2. **Mi cuenta** (los 4 roles) — no existe backend (ni perfil, ni cambio de contraseña).
3. **Dashboard con indicadores** (admin y coordinador) — no existe endpoint de estadísticas; cualquier número mostrado que no derive de datos reales ya cargados sería inventado.
4. **Importación Excel** (coordinador) — no existe ningún endpoint; la entidad existe en la base de datos pero no tiene capa de aplicación ni API.
5. **"Mi jornada" / "Mis servicios" del CONDUCTOR y "Mis servicios" del EMPLEADO** — no existe ningún endpoint de descubrimiento por usuario autenticado; construir estas pantallas hoy obligaría a inventar un endpoint o a simular datos.
6. **Temporizador de llegada en tiempo real** (conductor) — no hay estado de backend que respalde una cuenta regresiva.
7. **Checklist de condiciones de finalización** (§35) — el backend no expone qué condiciones faltan, solo acepta/rechaza.
8. **Búsqueda, filtros y paginación de servidor** en cualquier tabla (empresas, empleados, conductores, sedes, programaciones, servicios) — ningún endpoint `GET` de lista acepta parámetros de consulta. Si se implementan, deben ser exclusivamente client-side sobre los datos ya traídos, nunca asumir un parámetro de API que no existe.
9. **"Publicar" por servicio individual** (§29) — la operación real publica la jornada completa, no un servicio suelto.
10. **Columnas "Sede" y "Horario" en la lista de empleados** (§19) — no son atributos del empleado; son atributos de cada `ProgramacionTransporte`.
11. **"Unidad operativa"/"Conductor"/"Estado" a nivel de Jornada** (§25) — esos datos viven en cada `Servicio`, no en la `Jornada`.
12. **Incidencias como pantalla del COORDINADOR** — el endpoint solo autoriza al conductor asignado; el coordinador no tiene acceso a esa lectura/escritura hoy.

---

## 4. Lo que sí está 100% listo para construir sin más decisiones

- Login + JWT + rutas protegidas + cierre de sesión (ya implementado).
- Tema claro/oscuro (ya implementado).
- ADMINISTRADOR_PLATAFORMA → Empresas: listar, crear (+ primer coordinador), detalle, activar/desactivar empresa, activar/desactivar/reactivar coordinadores (ya implementado).
- COORDINADOR → Empleados: listar, ver detalle, actualizar, activar/desactivar (falta construir; endpoint completo y sin ambigüedades).
- COORDINADOR → Conductores: listar, ver detalle, registrar, vincular/desvincular, vehículos, unidades operativas (endpoint completo).
- COORDINADOR → Sedes: listar, crear, ver, actualizar, activar/desactivar (endpoint completo, campos coinciden exactamente con la maqueta del §22).
- COORDINADOR → Programaciones: crear, listar, ver, actualizar (endpoint completo).
- COORDINADOR → Jornadas y Servicios: crear, listar, ver, cambiar estado, asignar/retirar unidad, propuesta de planificación, reordenar pasajeros, publicar jornada (endpoint completo, con las correcciones de granularidad señaladas arriba).
- COORDINADOR → Pasajeros de un servicio: crear (asignar programación), listar, confirmar, marcar no-asistirá (endpoint completo).
- CONDUCTOR → activar/desactivar su propio perfil, gestionar sus vehículos y unidades operativas, generar códigos de activación para empleados (endpoint completo) — todo esto **sí** se puede construir ya, aunque no resuelva el flujo operativo diario completo.
- Chat individual conductor↔empleado sobre un `ServicioPasajero` ya conocido (endpoint completo).
- Incidencias y evidencias registradas por el conductor asignado (endpoint completo).
- Notificaciones del propio usuario (listar, marcar leída) — endpoint completo, no mencionado explícitamente en la spec pero disponible y reutilizable como bandeja de avisos genérica.

---

## 5. Decisiones que se necesitan antes de seguir (no se han inventado, quedan pendientes)

1. ¿Se agrega ahora un endpoint de "servicios/jornada del conductor autenticado hoy" y otro de "servicios del empleado autenticado" al backend? Sin esto, los roles CONDUCTOR y EMPLEADO no tienen una app operativa real, solo acciones puntuales sobre IDs ya conocidos.
2. ¿Se prioriza construir "Configuración global" y "Mi cuenta" en el backend, o se elimina esas entradas de menú del frontend hasta que existan?
3. ¿Se acepta que "Publicar" sea una acción a nivel de Jornada completa (como ya está implementado) en lugar de por Servicio individual, ajustando el diseño visual del §29?
4. ¿Se acepta mostrar la lista de Empleados sin columnas "Sede"/"Horario" (porque no son atributos del empleado), sustituyéndolas por Teléfono/Barrio?
5. Para la ficha de empleado (§20, sección "Información de transporte"), ¿se acepta la limitación de filtrar client-side sobre `GET /api/empresas/{id}/programaciones`, o se solicita un endpoint filtrado por `empleadoId`?
6. Para "Importación Excel", el formato del Excel también está pendiente según `AGENTS.md` §27 — ¿se construye primero el backend (controlador + servicio) antes de tocar esta pantalla?

---

## 6. Recomendación de orden de implementación

Dado lo anterior, el orden que respeta "no inventar" y aporta valor incremental real:

1. **COORDINADOR → Sedes** (backend 100% completo, campos ya coinciden con la maqueta).
2. **COORDINADOR → Empleados** (backend 100% completo, con las columnas corregidas de la tabla 2.2).
3. **COORDINADOR → Conductores + Vehículos + Unidades operativas** (backend 100% completo).
4. **COORDINADOR → Programaciones** (backend 100% completo).
5. **COORDINADOR → Jornadas → Servicios → Pasajeros → Planificación/Asignación → Publicar jornada** (backend 100% completo, es el flujo más grande; conviene dividirlo en varias tareas).
6. **CONDUCTOR/EMPLEADO**: quedan bloqueados en la parte de "descubrimiento" (mi jornada / mis servicios) hasta la decisión del punto 5.1. Mientras tanto, sí se puede construir ya el perfil propio del conductor (vehículos, unidades operativas, activar/desactivar) porque no depende de ese endpoint faltante.
7. Dashboard, Configuración global, Mi cuenta, Importación Excel: pendientes de decisión de backend antes de tocar frontend.

Fin de la auditoría.
