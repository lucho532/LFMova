using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TransportApp.Application.DTOs.Autenticacion;
using TransportApp.Application.DTOs.Conductores;
using TransportApp.Application.DTOs.Empresas;
using TransportApp.Application.DTOs.Jornadas;
using TransportApp.Application.DTOs.Programaciones;
using TransportApp.Application.DTOs.Sedes;
using TransportApp.Application.DTOs.Servicios;
using TransportApp.Application.DTOs.ServiciosPasajero;
using TransportApp.Application.DTOs.UnidadesOperativas;
using TransportApp.Application.DTOs.Vehiculos;
using TransportApp.Application.Interfaces;
using TransportApp.Domain.Entities;
using TransportApp.Domain.Enums;
using TransportApp.Infrastructure.Data;
using TransportApp.IntegrationTests.Fixtures;

namespace TransportApp.IntegrationTests;

/// <summary>
/// Ejercita el flujo funcional completo de la plataforma mediante HTTP real
/// contra PostgreSQL (ver <c>tasks.md</c> T118): Empresa → Coordinador →
/// Empleado → Programación → Conductor → Vehículo → Unidad → Jornada →
/// Servicio → ServicioPasajero → Publicación → Ejecución → Finalización.
/// </summary>
/// <remarks>
/// El coordinador establece su contraseña con el enlace enviado por correo
/// (el correo de pruebas es un doble que conserva los mensajes) y el
/// conductor se autorregistra y confirma su correo antes de que el
/// coordinador le asigne el rol. No existe un endpoint para crear un
/// <c>Empleado</c> (solo se crea mediante importación Excel, bloqueada por
/// T054), así que el empleado se siembra directamente. El resto del
/// flujo (sede, programación, conductor, vehículo, unidad, jornada, servicio,
/// pasajero, publicación, ejecución y finalización) se ejercita íntegramente
/// mediante HTTP real.
/// </remarks>
[Collection(IntegrationTestCollection.Nombre)]
public class FlujoCompletoTests
{
    private readonly IntegrationTestFixture _fixture;

    /// <summary>Crea la prueba con la colección compartida de PostgreSQL.</summary>
    public FlujoCompletoTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task FlujoCompleto_DesdeCreacionDeEmpresaHastaFinalizacionDelServicio()
    {
        using var alcance = _fixture.CrearAlcance();
        var contexto = alcance.ServiceProvider.GetRequiredService<TransportAppDbContext>();
        var hasheador = alcance.ServiceProvider.GetRequiredService<IHasheadorContrasenas>();

        // --- Bootstrap: administrador de plataforma (sin endpoint de autorregistro; se siembra directamente). ---
        await SemillaDatosHelper.CrearUsuarioConRolAsync(contexto, hasheador, "E2E-ADMIN", "ClaveAdmin123", Rol.ADMINISTRADOR_PLATAFORMA, empresaId: null);

        using var cliente = _fixture.Fabrica.CreateClient();
        await AutenticarAsync(cliente, "E2E-ADMIN", "ClaveAdmin123");

        // --- Empresa + su primer coordinador (admin crea ambos en la misma operación) ---
        var empresa = await PostAsync<CrearEmpresaDto, EmpresaDto>(cliente, "/api/empresas", new CrearEmpresaDto
        {
            Nombre = "Empresa Flujo Completo",
            Cif = "CIF-E2E",
            Direccion = "Calle Falsa 123",
            CedulaCoordinador = "E2E-COORD",
            NombreCoordinador = "Cora Coordinadora",
            TelefonoCoordinador = "3000000001",
            CorreoCoordinador = "coord.e2e@pruebas.test"
        });

        // El coordinador establece su contraseña mediante el enlace recibido por correo.
        var activacion = await cliente.PostAsJsonAsync("/api/autenticacion/restablecer-contrasena", new RestablecerContrasenaDto
        {
            Token = _fixture.Fabrica.Correo.ObtenerUltimoToken("coord.e2e@pruebas.test"),
            NuevaContrasena = "ClaveCoord123",
            ConfirmacionContrasena = "ClaveCoord123"
        });
        activacion.EnsureSuccessStatusCode();

        await AutenticarAsync(cliente, "E2E-COORD", "ClaveCoord123");

        // --- Sede (coordinador) ---
        var sede = await PostAsync<CrearSedeDto, SedeDto>(cliente, $"/api/empresas/{empresa.EmpresaId}/sedes", new CrearSedeDto
        {
            Nombre = "Sede Principal",
            Direccion = "Calle 1 # 2-3",
            Ciudad = "Bogotá",
            Barrio = "Centro"
        });

        // --- Conductor + Vehículo + Unidad Operativa (coordinador; sin endpoint de activación para conductor, se siembra la contraseña) ---
        // La persona se autorregistra y confirma su correo; el coordinador la busca por cédula y le asigna el rol de conductor.
        using (var clienteAnonimo = _fixture.Fabrica.CreateClient())
        {
            (await clienteAnonimo.PostAsJsonAsync("/api/autenticacion/registrarse", new RegistrarUsuarioDto
            {
                Correo = "cond.e2e@pruebas.test",
                NombreCompleto = "Carlos Conductor",
                Cedula = "E2E-COND",
                Telefono = "3000000000",
                Password = "ClaveCond123"
            })).EnsureSuccessStatusCode();
            (await clienteAnonimo.PostAsJsonAsync("/api/autenticacion/confirmar-correo",
                new ConfirmarCorreoDto { Token = _fixture.Fabrica.Correo.ObtenerUltimoToken("cond.e2e@pruebas.test") })).EnsureSuccessStatusCode();
        }

        await InvitacionesHelper.UnirAEmpresaAsync(_fixture.Fabrica, cliente, empresa.EmpresaId, "E2E-COND");
        var conductor = await PostAsync<CrearConductorDto, ConductorDto>(cliente, $"/api/empresas/{empresa.EmpresaId}/conductores", new CrearConductorDto
        {
            Cedula = "E2E-COND",
            Vehiculo = new CrearVehiculoDto
            {
                Placa = "E2E123",
                Marca = "Chevrolet",
                Modelo = "2022",
                Capacidad = 4,
                VigenciaSoat = new DateOnly(2027, 1, 1),
                VigenciaTecnomecanica = new DateOnly(2027, 6, 1)
            }
        });

        // Al crear al conductor se forma su unidad operativa (conductor + vehículo).
        var unidades = await cliente.GetFromJsonAsync<List<UnidadOperativaDto>>($"/api/conductores/{conductor.ConductorId}/unidades-operativas");
        var unidad = Assert.Single(unidades!);

        // --- Empleado (sin endpoint de creación: solo existe vía importación Excel, bloqueada por T054; se siembra directamente) ---
        var empleado = await SembrarEmpleadoAsync(contexto, hasheador, empresa.EmpresaId, "E2E-EMPL");

        // --- Programación de transporte (coordinador) ---
        var fecha = new DateOnly(2026, 3, 2);
        var hora = new TimeOnly(7, 0);
        var programacion = await PostAsync<CrearProgramacionDto, ProgramacionDto>(cliente, $"/api/empresas/{empresa.EmpresaId}/programaciones", new CrearProgramacionDto
        {
            EmpleadoId = empleado.EmpleadoId,
            SedeId = sede.SedeId,
            Fecha = fecha,
            Hora = hora,
            Tipo = TipoServicio.ENTRADA,
            DireccionRecogida = "Calle 10 # 20-30",
            BarrioRecogida = "Chapinero"
        });

        // --- Jornada (coordinador) ---
        var jornada = await PostAsync<CrearJornadaDto, JornadaDto>(cliente, $"/api/empresas/{empresa.EmpresaId}/jornadas", new CrearJornadaDto { FechaOperativa = fecha });

        // --- Servicio, con unidad asignada desde su creación (coordinador) ---
        var servicio = await PostAsync<CrearServicioDto, ServicioDto>(cliente, $"/api/empresas/{empresa.EmpresaId}/jornadas/{jornada.JornadaId}/servicios", new CrearServicioDto
        {
            UnidadOperativaId = unidad.UnidadOperativaId,
            SedeId = sede.SedeId,
            Fecha = fecha,
            HoraProgramada = hora,
            Tipo = TipoServicio.ENTRADA
        });

        // --- Pasajero del servicio (coordinador) ---
        var servicioPasajero = await PostAsync<CrearServicioPasajeroDto, ServicioPasajeroDto>(
            cliente,
            $"/api/empresas/{empresa.EmpresaId}/jornadas/{jornada.JornadaId}/servicios/{servicio.ServicioId}/pasajeros",
            new CrearServicioPasajeroDto { ProgramacionTransporteId = programacion.ProgramacionTransporteId });

        // --- Resolver estado del servicio hasta PUBLICADO y publicar la jornada (coordinador) ---
        var servicioServicio = alcance.ServiceProvider.GetRequiredService<IServicioServicio>();
        await servicioServicio.CambiarEstadoAsync(empresa.EmpresaId, servicio.ServicioId, new CambiarEstadoServicioDto { NuevoEstado = EstadoServicio.PENDIENTE_ASIGNACION });
        await servicioServicio.CambiarEstadoAsync(empresa.EmpresaId, servicio.ServicioId, new CambiarEstadoServicioDto { NuevoEstado = EstadoServicio.ASIGNADO });

        var respuestaPublicar = await cliente.PostAsync($"/api/empresas/{empresa.EmpresaId}/jornadas/{jornada.JornadaId}/publicar", content: null);
        respuestaPublicar.EnsureSuccessStatusCode();

        // --- Ejecución del servicio como el propio conductor ---
        await AutenticarAsync(cliente, "E2E-COND", "ClaveCond123");

        var respuestaIniciar = await cliente.PostAsync($"/api/empresas/{empresa.EmpresaId}/jornadas/{jornada.JornadaId}/servicios/{servicio.ServicioId}/iniciar", content: null);
        Assert.Equal(HttpStatusCode.NoContent, respuestaIniciar.StatusCode);

        var rutaPasajero = $"/api/empresas/{empresa.EmpresaId}/jornadas/{jornada.JornadaId}/servicios/{servicio.ServicioId}/pasajeros/{servicioPasajero.ServicioPasajeroId}";

        var respuestaLlegada = await cliente.PostAsync($"{rutaPasajero}/marcar-llegada", content: null);
        Assert.Equal(HttpStatusCode.NoContent, respuestaLlegada.StatusCode);

        // El empleado ve la misma hora de llegada que usa el conductor para su cronómetro, y le llega una notificación.
        var pasajeroTrasLlegada = await contexto.ServiciosPasajero.AsNoTracking().FirstAsync(p => p.ServicioPasajeroId == servicioPasajero.ServicioPasajeroId);
        Assert.NotNull(pasajeroTrasLlegada.HoraLlegadaConductor);
        var notificacionesEmpleado = await contexto.Notificaciones.AsNoTracking().Where(n => n.UsuarioId == empleado.UsuarioId).ToListAsync();
        Assert.Contains(notificacionesEmpleado, n => n.Tipo == "CONDUCTOR_LLEGO");

        // El empleado comparte su ubicación exacta antes de que el conductor lo marque recogido.
        using (var clienteEmpleado = _fixture.Fabrica.CreateClient())
        {
            await AutenticarAsync(clienteEmpleado, "E2E-EMPL", "ClaveEmpleado123");
            (await clienteEmpleado.PutAsJsonAsync($"{rutaPasajero}/ubicacion", new CompartirUbicacionDto { Latitud = 4.65, Longitud = -74.05 })).EnsureSuccessStatusCode();
        }
        await CambiarEstadoPasajeroAsync(cliente, rutaPasajero, EstadoServicioPasajero.RECOGIDO);

        // Al recogerlo por primera vez con una ubicación compartida, queda guardada automáticamente y se le notifica.
        var ubicacionGuardada = await cliente.GetFromJsonAsync<List<UbicacionAnteriorDto>>($"{rutaPasajero}/ubicaciones-anteriores");
        var guardada = Assert.Single(ubicacionGuardada!);
        Assert.Equal(4.65, guardada.Latitud);
        var notificacionesTrasRecogido = await contexto.Notificaciones.AsNoTracking().Where(n => n.UsuarioId == empleado.UsuarioId).ToListAsync();
        Assert.Contains(notificacionesTrasRecogido, n => n.Tipo == "PASAJERO_RECOGIDO");

        var respuestaFinalizar = await cliente.PostAsJsonAsync(
            $"/api/empresas/{empresa.EmpresaId}/jornadas/{jornada.JornadaId}/servicios/{servicio.ServicioId}/finalizar",
            new FinalizarServicioDto { Latitud = 4.6, Longitud = -74.1 });
        Assert.Equal(HttpStatusCode.NoContent, respuestaFinalizar.StatusCode);

        // --- Verificación final ---
        var servicioFinal = await contexto.Servicios.AsNoTracking().FirstAsync(s => s.ServicioId == servicio.ServicioId);
        Assert.Equal(EstadoServicio.FINALIZADO, servicioFinal.Estado);
        Assert.NotNull(servicioFinal.HoraFinReal);
        Assert.Equal(4.6, servicioFinal.LatitudFinalizacion);
        Assert.Equal(-74.1, servicioFinal.LongitudFinalizacion);

        var pasajeroFinal = await contexto.ServiciosPasajero.AsNoTracking().FirstAsync(p => p.ServicioPasajeroId == servicioPasajero.ServicioPasajeroId);
        Assert.Equal(EstadoServicioPasajero.RECOGIDO, pasajeroFinal.Estado);

        // Tras finalizar la ruta, el chat de ese pasajero queda solo de lectura: no se pueden enviar más mensajes.
        var mensajeTrasFinalizar = await cliente.PostAsJsonAsync($"{rutaPasajero}/mensajes", new { contenido = "¿Ya llegamos?" });
        Assert.Equal(HttpStatusCode.Conflict, mensajeTrasFinalizar.StatusCode);
    }

    private async Task AutenticarAsync(HttpClient cliente, string cedula, string password)
    {
        var respuesta = await cliente.PostAsJsonAsync("/api/autenticacion/iniciar-sesion", new IniciarSesionDto { Identificador = cedula, Password = password });
        respuesta.EnsureSuccessStatusCode();
        var cuerpo = await respuesta.Content.ReadFromJsonAsync<RespuestaAutenticacionDto>();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", cuerpo!.Token);
    }

    private static async Task<TRespuesta> PostAsync<TSolicitud, TRespuesta>(HttpClient cliente, string ruta, TSolicitud cuerpo)
    {
        var respuesta = await cliente.PostAsJsonAsync(ruta, cuerpo);
        respuesta.EnsureSuccessStatusCode();
        return (await respuesta.Content.ReadFromJsonAsync<TRespuesta>())!;
    }

    private static async Task CambiarEstadoPasajeroAsync(HttpClient cliente, string rutaPasajero, EstadoServicioPasajero nuevoEstado)
    {
        var respuesta = await cliente.PutAsJsonAsync($"{rutaPasajero}/estado", new CambiarEstadoServicioPasajeroDto { NuevoEstado = nuevoEstado });
        respuesta.EnsureSuccessStatusCode();
    }

    private static async Task<Empleado> SembrarEmpleadoAsync(TransportAppDbContext contexto, IHasheadorContrasenas hasheador, int empresaId, string cedula)
    {
        var usuario = new Usuario { Cedula = cedula, NombreCompleto = "Emilia Empleada", PasswordHash = hasheador.Hashear("ClaveEmpleado123"), CorreoConfirmado = true, Activo = true };
        contexto.Usuarios.Add(usuario);
        await contexto.SaveChangesAsync();

        contexto.UsuarioRoles.Add(new UsuarioRol { UsuarioId = usuario.UsuarioId, Rol = Rol.EMPLEADO, EmpresaId = empresaId, Activo = true });

        var empleado = new Empleado
        {
            UsuarioId = usuario.UsuarioId,
            EmpresaId = empresaId,
            NombreCompleto = "Emilia Empleada",
            Telefono = "3111111111",
            Direccion = "Carrera 5 # 6-7",
            Barrio = "Chapinero",
            Activo = true
        };
        contexto.Empleados.Add(empleado);
        await contexto.SaveChangesAsync();

        return empleado;
    }
}
