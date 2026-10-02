using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using LFMova.Application.DTOs.Autenticacion;
using LFMova.Application.DTOs.Empleados;
using LFMova.Application.Interfaces;
using LFMova.Domain.Entities;
using LFMova.Domain.Enums;
using LFMova.Infrastructure.Data;
using LFMova.IntegrationTests.Fixtures;

namespace LFMova.IntegrationTests;

/// <summary>
/// Prueba contra PostgreSQL el borrado en cascada de una persona desde la
/// gestión de empleados: con claves foráneas restrictivas, el orden del
/// borrado solo se puede comprobar contra la base real.
/// </summary>
[Collection(IntegrationTestCollection.Nombre)]
public class EliminacionPersonaTests
{
    private readonly IntegrationTestFixture _fixture;

    /// <summary>Crea la prueba con la colección compartida de PostgreSQL.</summary>
    public EliminacionPersonaTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Eliminar_BorraTodoRastroDeLaPersona_CuandoSoloEsDeEstaEmpresa()
    {
        var (empresaId, empleadoId, usuarioId, servicioQueConduceId) = await SembrarPersonaConHistorialAsync("ELIM-A", otraEmpresa: false);
        using var coordinador = _fixture.Fabrica.CreateClient();
        await AutenticarAsync(coordinador, "ELIM-A-COORD", "ClaveCoord123");

        var respuesta = await coordinador.DeleteAsync($"/api/empresas/{empresaId}/empleados/{empleadoId}");

        respuesta.EnsureSuccessStatusCode();
        Assert.True((await respuesta.Content.ReadFromJsonAsync<ResultadoEliminacionPersonaDto>())!.CuentaEliminada);

        using var alcance = _fixture.CrearAlcance();
        var contexto = alcance.ServiceProvider.GetRequiredService<LFMovaDbContext>();
        Assert.False(await contexto.Usuarios.AnyAsync(u => u.UsuarioId == usuarioId));
        Assert.False(await contexto.Usuarios.AnyAsync(u => u.Cedula == "ELIM-A-PERS"));
        Assert.False(await contexto.Empleados.AnyAsync(e => e.UsuarioId == usuarioId));
        Assert.False(await contexto.Conductores.AnyAsync(c => c.UsuarioId == usuarioId));
        Assert.False(await contexto.UsuarioRoles.AnyAsync(r => r.UsuarioId == usuarioId));
        Assert.False(await contexto.Vehiculos.AnyAsync(v => v.Placa == "ELIM-A"));
        Assert.False(await contexto.ServiciosPasajero.AnyAsync(p => p.EmpleadoId == empleadoId));
        Assert.False(await contexto.Mensajes.AnyAsync(m => m.UsuarioId == usuarioId));

        // La ruta que conducía sigue existiendo, pero vuelve a quedar pendiente de asignar.
        var servicio = await contexto.Servicios.SingleAsync(s => s.ServicioId == servicioQueConduceId);
        Assert.Null(servicio.UnidadOperativaId);
        Assert.Equal(EstadoServicio.PENDIENTE_ASIGNACION, servicio.Estado);
    }

    [Fact]
    public async Task Eliminar_SoloQuitaALaPersonaDeLaEmpresa_CuandoTambienEsConductoraDeOtra()
    {
        var (empresaId, empleadoId, usuarioId, _) = await SembrarPersonaConHistorialAsync("ELIM-B", otraEmpresa: true);
        using var coordinador = _fixture.Fabrica.CreateClient();
        await AutenticarAsync(coordinador, "ELIM-B-COORD", "ClaveCoord123");

        var respuesta = await coordinador.DeleteAsync($"/api/empresas/{empresaId}/empleados/{empleadoId}");

        respuesta.EnsureSuccessStatusCode();
        Assert.False((await respuesta.Content.ReadFromJsonAsync<ResultadoEliminacionPersonaDto>())!.CuentaEliminada);

        using var alcance = _fixture.CrearAlcance();
        var contexto = alcance.ServiceProvider.GetRequiredService<LFMovaDbContext>();
        Assert.True(await contexto.Usuarios.AnyAsync(u => u.UsuarioId == usuarioId));
        Assert.False(await contexto.Empleados.AnyAsync(e => e.UsuarioId == usuarioId));
        Assert.True(await contexto.Conductores.AnyAsync(c => c.UsuarioId == usuarioId));
        var vinculacion = await contexto.VinculacionesConductorEmpresa.SingleAsync(v => v.Conductor!.UsuarioId == usuarioId);
        Assert.NotEqual(empresaId, vinculacion.EmpresaId);
        var rolEmpleado = await contexto.UsuarioRoles.SingleAsync(r => r.UsuarioId == usuarioId && r.Rol == Rol.EMPLEADO);
        Assert.Null(rolEmpleado.EmpresaId);
    }

    [Fact]
    public async Task Eliminar_RespondeConflicto_CuandoElEmpleadoEsDeOtraEmpresa()
    {
        var (_, empleadoId, usuarioId, _) = await SembrarPersonaConHistorialAsync("ELIM-C", otraEmpresa: false);
        int otraEmpresaId;
        using (var alcance = _fixture.CrearAlcance())
        {
            var contexto = alcance.ServiceProvider.GetRequiredService<LFMovaDbContext>();
            var hasheador = alcance.ServiceProvider.GetRequiredService<IHasheadorContrasenas>();
            otraEmpresaId = (await SemillaDatosHelper.CrearEmpresaAsync(contexto, "ELIM-C ajena")).EmpresaId;
            await SemillaDatosHelper.CrearUsuarioConRolAsync(contexto, hasheador, "ELIM-C-AJENO", "ClaveCoord123", Rol.COORDINADOR, otraEmpresaId);
        }

        using var ajeno = _fixture.Fabrica.CreateClient();
        await AutenticarAsync(ajeno, "ELIM-C-AJENO", "ClaveCoord123");

        var respuesta = await ajeno.DeleteAsync($"/api/empresas/{otraEmpresaId}/empleados/{empleadoId}");

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
        using var verificacion = _fixture.CrearAlcance();
        Assert.True(await verificacion.ServiceProvider.GetRequiredService<LFMovaDbContext>().Usuarios.AnyAsync(u => u.UsuarioId == usuarioId));
    }

    /// <summary>
    /// Crea una empresa con su coordinador y una persona que es a la vez
    /// empleada y conductora: fue pasajera de una ruta finalizada (con chat,
    /// incidencia y evidencia) y conduce otra ruta publicada. Con
    /// <paramref name="otraEmpresa"/> queda además vinculada como conductora a
    /// una segunda empresa.
    /// </summary>
    private async Task<(int EmpresaId, int EmpleadoId, int UsuarioId, int ServicioQueConduceId)> SembrarPersonaConHistorialAsync(string prefijo, bool otraEmpresa)
    {
        using var alcance = _fixture.CrearAlcance();
        var contexto = alcance.ServiceProvider.GetRequiredService<LFMovaDbContext>();
        var hasheador = alcance.ServiceProvider.GetRequiredService<IHasheadorContrasenas>();

        var empresa = await SemillaDatosHelper.CrearEmpresaAsync(contexto, $"{prefijo} empresa");
        await SemillaDatosHelper.CrearUsuarioConRolAsync(contexto, hasheador, $"{prefijo}-COORD", "ClaveCoord123", Rol.COORDINADOR, empresa.EmpresaId);
        var usuario = await SemillaDatosHelper.CrearUsuarioConRolAsync(contexto, hasheador, $"{prefijo}-PERS", "ClavePers123", Rol.EMPLEADO, empresa.EmpresaId);
        contexto.UsuarioRoles.Add(new UsuarioRol { UsuarioId = usuario.UsuarioId, Rol = Rol.CONDUCTOR, EmpresaId = null, Activo = true });

        var empleado = new Empleado { UsuarioId = usuario.UsuarioId, EmpresaId = empresa.EmpresaId, NombreCompleto = "Persona", Telefono = "300", Direccion = "Calle 1", Barrio = "Centro", Activo = true };
        var conductor = new Conductor { UsuarioId = usuario.UsuarioId, NombreCompleto = "Persona", Telefono = "300", Activo = true };
        conductor.VinculacionesConductorEmpresa.Add(new VinculacionConductorEmpresa { EmpresaId = empresa.EmpresaId, Activa = true });
        if (otraEmpresa)
        {
            var segunda = await SemillaDatosHelper.CrearEmpresaAsync(contexto, $"{prefijo} segunda");
            conductor.VinculacionesConductorEmpresa.Add(new VinculacionConductorEmpresa { EmpresaId = segunda.EmpresaId, Activa = true });
        }

        var vehiculo = new Vehiculo { Conductor = conductor, Placa = prefijo, Marca = "Kia", Modelo = "Carnival", Capacidad = 4, Activo = true };
        var unidad = new UnidadOperativa { Conductor = conductor, Vehiculo = vehiculo, Activa = true };
        var sede = new Sede { EmpresaId = empresa.EmpresaId, Nombre = $"{prefijo} sede", Direccion = "Calle 2", Ciudad = "Manizales", Barrio = "Centro", Activa = true };
        var jornada = new Jornada { EmpresaId = empresa.EmpresaId, FechaOperativa = new DateOnly(2026, 1, 20) };
        contexto.AddRange(empleado, unidad, sede, jornada);
        await contexto.SaveChangesAsync();

        var fecha = new DateOnly(2026, 1, 20);
        var comoPasajera = new Servicio { JornadaId = jornada.JornadaId, SedeId = sede.SedeId, Fecha = fecha, HoraProgramada = new TimeOnly(6, 0), Tipo = TipoServicio.ENTRADA, Estado = EstadoServicio.FINALIZADO };
        var queConduce = new Servicio { JornadaId = jornada.JornadaId, SedeId = sede.SedeId, UnidadOperativaId = unidad.UnidadOperativaId, Fecha = fecha, HoraProgramada = new TimeOnly(18, 0), Tipo = TipoServicio.SALIDA, Estado = EstadoServicio.PUBLICADO };
        var programacion = new ProgramacionTransporte { EmpresaId = empresa.EmpresaId, Empleado = empleado, SedeId = sede.SedeId, Fecha = fecha, Hora = new TimeOnly(6, 0), Tipo = TipoServicio.ENTRADA, DireccionRecogida = "Calle 1", BarrioRecogida = "Centro" };
        var pasajero = new ServicioPasajero { Servicio = comoPasajera, ProgramacionTransporte = programacion, Empleado = empleado, Estado = EstadoServicioPasajero.NO_RECOGIDO, Orden = 1, DireccionRecogida = "Calle 1" };
        var conversacion = new Conversacion { ServicioPasajero = pasajero };
        conversacion.Mensajes.Add(new Mensaje { UsuarioId = usuario.UsuarioId, Contenido = "Ya salgo", FechaHora = DateTime.UtcNow });
        var incidencia = new Incidencia { ServicioPasajero = pasajero, Tipo = TipoIncidencia.OTRA, Descripcion = "Prueba", FechaHora = DateTime.UtcNow };
        var evidencia = new Evidencia { Incidencia = incidencia, ReferenciaArchivo = $"evidencias/{prefijo}.jpg", FechaHora = DateTime.UtcNow };
        contexto.AddRange(queConduce, conversacion, evidencia);
        contexto.UbicacionesRecogidaHistorica.Add(new UbicacionRecogidaHistorica { Empleado = empleado, Direccion = "Calle 1", Barrio = "Centro", FechaRegistro = DateTime.UtcNow });
        contexto.Notificaciones.Add(new Notificacion { UsuarioId = usuario.UsuarioId, Tipo = "PRUEBA", Titulo = "Hola", Mensaje = "Hola", FechaHora = DateTime.UtcNow });
        await contexto.SaveChangesAsync();

        return (empresa.EmpresaId, empleado.EmpleadoId, usuario.UsuarioId, queConduce.ServicioId);
    }

    private static async Task AutenticarAsync(HttpClient cliente, string identificador, string password)
    {
        var respuesta = await cliente.PostAsJsonAsync("/api/autenticacion/iniciar-sesion", new IniciarSesionDto { Identificador = identificador, Password = password });
        respuesta.EnsureSuccessStatusCode();
        var cuerpo = await respuesta.Content.ReadFromJsonAsync<RespuestaAutenticacionDto>();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", cuerpo!.Token);
    }
}
