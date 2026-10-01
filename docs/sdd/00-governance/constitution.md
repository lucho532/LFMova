# Constitución del Proyecto — Plataforma de Transporte Empresarial

## 1. Propósito

Este proyecto es una plataforma de gestión y operación de transporte empresarial orientada a empresas que requieren transportar empleados entre sus ubicaciones de recogida y las sedes empresariales.

La plataforma debe permitir:

* Gestionar múltiples empresas.
* Gestionar empleados, conductores, vehículos y sedes.
* Importar información de programación mediante archivos Excel.
* Crear y organizar servicios de transporte.
* Agrupar pasajeros en servicios.
* Asignar servicios a unidades operativas.
* Organizar jornadas de trabajo de los conductores.
* Publicar servicios a conductores y empleados.
* Ejecutar los servicios en tiempo real.
* Gestionar estados de pasajeros.
* Gestionar incidencias y evidencias.
* Permitir comunicación individual entre conductor y empleado.
* Mantener históricos operativos sin alterar información histórica.

---

# 2. Principios obligatorios

## 2.1. Código en español

Todo el código nuevo debe utilizar nombres en español.

Esto incluye:

* Clases.
* Interfaces.
* Métodos.
* Propiedades.
* Variables.
* Parámetros.
* DTOs.
* Enumeraciones.
* Servicios.
* Repositorios.
* Controladores.
* Validadores.
* Mappers.
* Utilidades.
* Excepciones propias.
* Comentarios.
* Documentación XML.

Ejemplo:

```text
Empleado
EmpleadoDto
CrearEmpleadoDto
ActualizarEmpleadoDto
IEmpleadoServicio
EmpleadoServicio
IEmpleadoRepositorio
EmpleadoRepositorio
EmpleadoMapper
EmpleadoValidador
```

No utilizar nombres en inglés cuando exista una denominación clara en español.

Los nombres propios de tecnologías externas pueden mantenerse según su nomenclatura oficial, por ejemplo:

```text
Entity Framework Core
PostgreSQL
ASP.NET Core
JWT
Swagger
Docker
xUnit
```

---

# 3. Documentación obligatoria de clases

Toda clase, interfaz, enum o componente de dominio relevante debe tener documentación XML en español.

La documentación debe explicar:

1. Qué representa.
2. Cuál es su responsabilidad.
3. Qué responsabilidades NO debe asumir.

Ejemplo:

```csharp
/// <summary>
/// Representa el servicio encargado de gestionar empleados.
/// Su responsabilidad es coordinar las operaciones relacionadas
/// con empleados y aplicar las reglas de negocio correspondientes.
/// No debe acceder directamente al contexto de Entity Framework
/// ni contener lógica propia de persistencia.
/// </summary>
public class EmpleadoServicio
{
}
```

La documentación no debe ser decorativa. Debe ayudar a comprender la arquitectura.

---

# 4. Arquitectura

La aplicación utilizará una arquitectura por capas con separación clara de responsabilidades.

Estructura principal:

```text
src/
├── TransportApp.Api/
│   └── Controllers/
│
├── TransportApp.Application/
│   ├── DTOs/
│   ├── Interfaces/
│   ├── Services/
│   ├── Implementations/
│   ├── Mappers/
│   ├── Validators/
│   └── Utils/
│
├── TransportApp.Domain/
│   ├── Entities/
│   ├── Enums/
│   └── Rules/
│
└── TransportApp.Infrastructure/
    ├── Data/
    └── Repositories/

tests/
├── Unit/
└── Integration/
```

El nombre definitivo de los proyectos podrá ajustarse durante el plan técnico, pero la separación conceptual debe mantenerse.

---

# 5. Flujo de dependencias

El flujo normal de una operación será:

```text
Controller
    ↓
IServicio
    ↓
Servicio
    ↓
IRepositorio
    ↓
Repositorio
    ↓
Entity Framework Core
    ↓
PostgreSQL
```

Para respuestas:

```text
Entity
    ↓
Mapper
    ↓
DTO
    ↓
Controller
    ↓
JSON
```

Los controladores no deben contener reglas de negocio.

Los repositorios no deben contener reglas de negocio propias de la aplicación.

Los DTOs no deben utilizarse como entidades de persistencia.

Las entidades de dominio no deben depender de ASP.NET Core.

---

# 6. Tecnologías base

La primera implementación utilizará:

* C#.
* ASP.NET Core Web API.
* Entity Framework Core.
* PostgreSQL.
* JWT.
* Swagger/OpenAPI.
* Docker.
* xUnit.
* Git.
* GitHub.

No introducir nuevas tecnologías, frameworks o patrones arquitectónicos importantes sin justificar su necesidad y documentar el cambio.

La simplicidad y mantenibilidad tienen prioridad sobre la cantidad de tecnologías utilizadas.

---

# 7. Multiempresa

La plataforma es multiempresa.

Toda operación debe respetar el aislamiento de datos entre empresas.

Un coordinador solamente puede acceder a información de su empresa.

El backend debe aplicar el aislamiento.

No se puede confiar exclusivamente en restricciones del frontend.

El hecho de que un usuario conozca un identificador de otra empresa no le concede acceso.

---

# 8. Roles

Existen cuatro roles:

```text
ADMINISTRADOR_PLATAFORMA
COORDINADOR
CONDUCTOR
EMPLEADO
```

### Administrador de plataforma

Puede:

* Crear empresas.
* Gestionar configuración global.
* Gestionar aspectos generales de la plataforma.

No pertenece obligatoriamente a una empresa.

No gestiona ni conoce la operativa o el personal de las empresas, incluyendo el cambio de empresa de un empleado, el cual se resuelve automáticamente durante la importación Excel (ver §10).

### Coordinador

Pertenece a una única empresa.

Un usuario no puede tener el rol `COORDINADOR` activo en más de una empresa simultáneamente. Esta restricción no limita que la misma persona tenga otros roles (`EMPLEADO`, `CONDUCTOR`) en distintos contextos según las reglas ya definidas.

Una empresa debe conservar siempre al menos un `COORDINADOR` activo; no se permite revocar el último coordinador activo de una empresa.

Puede gestionar la operación de su empresa:

* Empleados.
* Conductores.
* Vehículos.
* Sedes.
* Importaciones.
* Programaciones.
* Jornadas.
* Servicios.
* Asignaciones.
* Publicaciones.

No puede crear empresas.

### Conductor

Es independiente de una empresa concreta.

Puede trabajar para varias empresas mediante:

```text
VinculacionConductorEmpresa
```

Solamente puede consultar y ejecutar los servicios que le correspondan.

### Empleado

Pertenece actualmente a una empresa.

Solamente puede consultar y gestionar su propia información y la información de transporte necesaria para él.

---

# 9. Identidad mediante cédula

La cédula identifica de forma única a una persona dentro de la plataforma.

La cédula:

* Es única globalmente.
* No puede duplicarse.
* No cambia al cambiar de empresa.
* No debe utilizarse como sustituto de las claves primarias internas.

Las relaciones entre entidades utilizarán IDs internos.

---

# 10. Cambio de empresa de un empleado

Debe existir un único registro de `Empleado` por cédula.

No existe una operación manual de transferencia de un empleado entre empresas.

El cambio de empresa se detecta y se ejecuta automáticamente durante la importación de un archivo Excel, cuando la misma `Cedula` (identificada mediante `Usuario`) aparece en la importación de una empresa distinta a la actual.

`ADMINISTRADOR_PLATAFORMA` no participa en este proceso ni en la operación o gestión del personal de las empresas. Su función se limita a la creación de empresas y a la asignación del primer coordinador.

Un `COORDINADOR` únicamente gestiona empleados de su propia empresa (consultar, actualizar, activar/desactivar); no puede iniciar ni ejecutar el cambio de empresa de un empleado.

Al detectarse el cambio, se actualizan en una misma transacción:

```text
Empleado.EmpresaId
UsuarioRol(EMPLEADO).EmpresaId
```

No se crea otro empleado con la misma cédula.

No se modifica información histórica.

Las programaciones, servicios y registros históricos deben conservar el contexto de la empresa donde ocurrieron.

No se implementará inicialmente una entidad independiente de historial de empresas del empleado.

---

# 11. Usuario y activación

La entidad `Usuario` representa la cuenta de acceso.

El campo:

```text
PasswordHash
```

puede ser `NULL`.

`PasswordHash = NULL` significa que la cuenta está creada pero todavía no ha sido activada.

No se utilizará inicialmente un campo `EstadoActivacion`.

`Usuario` no contiene un campo `EmpresaId`.

La empresa asociada a un rol se resuelve exclusivamente mediante:

```text
Usuario
   ↓
UsuarioRol
   ↓
Empresa
```

`UsuarioRol.EmpresaId` es:

* `NULL` para `ADMINISTRADOR_PLATAFORMA` (rol global).
* obligatorio para `COORDINADOR`, correspondiente a la empresa que administra.
* obligatorio para `EMPLEADO`, correspondiente a su empresa actual.
* `NULL` para `CONDUCTOR`: la relación con cada empresa se resuelve exclusivamente mediante `VinculacionConductorEmpresa`, no mediante `UsuarioRol.EmpresaId` ni mediante `Usuario.EmpresaId`. No se crea un `UsuarioRol CONDUCTOR` por cada empresa vinculada.

---

# 12. Histórico operativo

La información histórica no debe depender exclusivamente del estado actual de una entidad.

Los datos que puedan cambiar con el tiempo y sean relevantes para una operación histórica deben quedar almacenados en el registro operativo correspondiente.

Por ejemplo:

```text
Empleado.Direccion
```

representa la dirección actual.

Mientras que:

```text
ServicioPasajero.DireccionRecogida
```

representa la dirección utilizada específicamente para ese servicio.

Modificar el empleado actualmente no debe modificar servicios históricos.

---

# 13. Entidades desactivables

Las entidades operativas que tengan información histórica no deben eliminarse físicamente cuando dejan de estar disponibles.

Se utilizará el concepto de:

```text
Activo
Activa
```

según corresponda.

Esto aplica, entre otras, a:

* Empresas.
* Empleados.
* Conductores.
* Vehículos.
* Sedes.
* Vinculaciones entre conductor y empresa.
* Unidades operativas.

La eliminación física deberá justificarse explícitamente.

---

# 14. Reglas de negocio en backend

Las reglas de negocio deben estar protegidas en el backend.

El frontend puede ayudar con validaciones de experiencia de usuario, pero nunca será la única barrera de seguridad o integridad.

Ejemplo:

```text
React → oculta botón
```

no es suficiente.

Debe existir:

```text
API → valida autorización y regla de negocio
```

---

# 15. Algoritmos de planificación

Los algoritmos de planificación y agrupación de rutas deben funcionar utilizando datos reales:

* Capacidad.
* Horarios.
* Coordenadas.
* Ubicaciones.
* Distancias.
* Tiempos estimados.
* Continuidad geográfica.
* Servicios anteriores y posteriores.
* Carga de trabajo.

No se deben implementar reglas basadas en listas rígidas de barrios.

El algoritmo debe generar recomendaciones.

El coordinador conserva la decisión final.

El sistema no debe modificar automáticamente una planificación manual del coordinador sin una regla explícita que lo autorice.

---

# 16. Estados

Los estados representan el estado actual de una entidad operativa.

No se debe crear automáticamente un historial de estados para cada entidad.

Para esta primera versión se utilizará únicamente el estado actual, salvo que una especificación posterior determine expresamente la necesidad de auditoría histórica.

---

# 17. Privacidad

Las conversaciones entre conductor y empleado son individuales.

No existe chat grupal como modelo predeterminado.

Un usuario solamente debe poder acceder a las conversaciones en las que participa y para las que tenga autorización.

La información de otros empleados debe limitarse a lo estrictamente necesario para ejecutar el servicio.

---

# 18. Simplicidad

No sobreingenierizar.

Antes de crear:

* Una nueva entidad.
* Un nuevo servicio.
* Un nuevo patrón.
* Una nueva abstracción.
* Una nueva tecnología.
* Una nueva tabla.

debe existir una necesidad funcional o técnica clara.

La arquitectura debe ser suficientemente sólida para crecer, pero no innecesariamente compleja.

---

# 19. Pruebas

Las reglas de negocio importantes deben tener pruebas automatizadas.

Se utilizarán:

```text
tests/Unit
tests/Integration
```

Las pruebas deben validar principalmente:

* Reglas de negocio.
* Autorización.
* Aislamiento entre empresas.
* Relaciones.
* Transiciones de estados.
* Operaciones críticas.
* Persistencia.
* Integración con PostgreSQL cuando corresponda.

---

# 20. Docker

La aplicación debe poder ejecutarse mediante Docker.

La configuración de Docker debe permitir reproducir el entorno de desarrollo y facilitar posteriormente el despliegue.

No se deben introducir dependencias externas innecesarias únicamente para dockerizar el proyecto.

---

# 21. Migraciones de base de datos

Entity Framework Core será responsable de gestionar el modelo de persistencia y las migraciones.

Las modificaciones estructurales de la base de datos deben realizarse mediante migraciones controladas.

No se deben realizar cambios manuales arbitrarios en producción como mecanismo habitual de evolución del esquema.

---

# 22. API

La API debe utilizar:

* HTTP semántico.
* DTOs.
* Validación de entrada.
* Respuestas coherentes.
* Autorización.
* Manejo controlado de errores.
* Swagger/OpenAPI.

No exponer directamente entidades de persistencia como contrato público de la API cuando un DTO sea apropiado.

---

# 23. Regla para Claude Code

Claude Code actúa como **implementador de las decisiones definidas en la especificación**.

No debe cambiar unilateralmente decisiones de negocio ya aprobadas.

Si detecta una contradicción, ambigüedad o requisito faltante que afecte significativamente la arquitectura o el comportamiento:

1. Debe identificar el problema.
2. Debe documentarlo.
3. Debe detener la implementación de esa parte.
4. Debe solicitar una decisión antes de inventar una regla de negocio.

No debe introducir funcionalidades no solicitadas bajo la premisa de que "podrían ser útiles".

---

# 24. Trazabilidad

Cada funcionalidad implementada debe poder relacionarse con:

```text
Requisito
   ↓
Especificación
   ↓
Plan
   ↓
Tarea
   ↓
Código
   ↓
Prueba
```

Las tareas deben ser suficientemente concretas para comprobar si fueron completadas.

---

# 25. Definición de terminado

Una tarea no se considera terminada únicamente porque el código compile.

Cuando corresponda, debe incluir:

* Implementación.
* Validaciones.
* Pruebas.
* Integración.
* Documentación.
* Migraciones.
* Actualización de contratos.
* Verificación de compilación.
* Verificación de pruebas.

Una tarea debe marcarse como completada solamente después de verificar sus criterios.

---

# 26. Regla de no regresión

Una modificación no debe romper funcionalidades previamente implementadas.

Antes de considerar una funcionalidad terminada se debe ejecutar la batería de pruebas relevante.

Cuando sea necesario, se deben agregar pruebas para evitar que el defecto reaparezca.

---

# 27. Principio final

El sistema debe priorizar:

```text
Claridad
↓
Correctitud
↓
Seguridad
↓
Mantenibilidad
↓
Escalabilidad
```

La solución más sencilla que cumpla correctamente las reglas de negocio será preferible a una solución más compleja.
