using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using LFMova.Application.DTOs.Autenticacion;
using LFMova.Application.DTOs.Empresas;
using LFMova.Application.Interfaces;
using LFMova.Domain.Entities;
using LFMova.Domain.Enums;
using LFMova.Infrastructure.Data;
using LFMova.IntegrationTests.Fixtures;

namespace LFMova.IntegrationTests;

/// <summary>
/// Prueba contra PostgreSQL la corrección y el borrado completo de una
/// empresa por el administrador: con claves foráneas restrictivas, el orden
/// del borrado solo se puede comprobar contra la base real.
/// </summary>
[Collection(IntegrationTestCollection.Nombre)]
public class EdicionEmpresaTests
{
    private readonly IntegrationTestFixture _fixture;

    /// <summary>Crea la prueba con la colección compartida de PostgreSQL.</summary>
    public EdicionEmpresaTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Actualizar_CorrigeLosDatosYRechazaUnCifRepetido()
    {
        var (empresaId, _) = await SembrarEmpresaConOperacionAsync("EDE-A");
        using var administrador = _fixture.Fabrica.CreateClient();
        await AutenticarAsync(administrador, "EDE-A-ADMIN", "ClaveAdmin123");

        var respuesta = await administrador.PutAsJsonAsync($"/api/empresas/{empresaId}",
            new ActualizarEmpresaDto { Nombre = "EDE-A corregida", Cif = "CIF-EDE-A-NUEVO", Direccion = "Carrera 9 # 1-2" });

        respuesta.EnsureSuccessStatusCode();
        var corregida = await respuesta.Content.ReadFromJsonAsync<EmpresaDto>();
        Assert.Equal("EDE-A corregida", corregida!.Nombre);
        Assert.Equal("CIF-EDE-A-NUEVO", corregida.Cif);

        // El CIF de la otra empresa sembrada no se puede reutilizar.
        var repetido = await administrador.PutAsJsonAsync($"/api/empresas/{empresaId}",
            new ActualizarEmpresaDto { Nombre = "EDE-A corregida", Cif = "CIF-EDE-A otra", Direccion = "Carrera 9 # 1-2" });
        Assert.Equal(HttpStatusCode.Conflict, repetido.StatusCode);
    }

    [Fact]
    public async Task Eliminar_BorraLaEmpresaConTodoSuHistorialYConservaLasCuentasActivadas()
    {
        var (empresaId, ids) = await SembrarEmpresaConOperacionAsync("EDE-B");
        using var administrador = _fixture.Fabrica.CreateClient();
        await AutenticarAsync(administrador, "EDE-B-ADMIN", "ClaveAdmin123");

        var respuesta = await administrador.DeleteAsync($"/api/empresas/{empresaId}");

        Assert.Equal(HttpStatusCode.NoContent, respuesta.StatusCode);
        using var alcance = _fixture.CrearAlcance();
        var contexto = alcance.ServiceProvider.GetRequiredService<LFMovaDbContext>();
        Assert.False(await contexto.Empresas.AnyAsync(e => e.EmpresaId == empresaId));
        Assert.False(await contexto.Jornadas.AnyAsync(j => j.EmpresaId == empresaId));
        Assert.False(await contexto.Sedes.AnyAsync(s => s.EmpresaId == empresaId));
        Assert.False(await contexto.Zonas.AnyAsync(z => z.EmpresaId == empresaId));
        Assert.False(await contexto.Empleados.AnyAsync(e => e.EmpresaId == empresaId));
        Assert.False(await contexto.UsosConductor.AnyAsync(u => u.EmpresaId == empresaId));
        Assert.False(await contexto.CierresMensuales.AnyAsync(c => c.EmpresaId == empresaId));
        Assert.False(await contexto.UsuarioRoles.AnyAsync(r => r.EmpresaId == empresaId));

        // El coordinador conserva su cuenta, pero ya no coordina nada; el conductor conserva su ficha y su vehículo.
        Assert.True(await contexto.Usuarios.AnyAsync(u => u.UsuarioId == ids.Coordinador));
        Assert.False(await contexto.UsuarioRoles.AnyAsync(r => r.UsuarioId == ids.Coordinador && r.Rol == Rol.COORDINADOR));
        Assert.True(await contexto.Conductores.AnyAsync(c => c.UsuarioId == ids.Conductor));
        Assert.True(await contexto.Vehiculos.AnyAsync(v => v.Placa == "EDE-B"));
        Assert.False(await contexto.VinculacionesConductorEmpresa.AnyAsync(v => v.EmpresaId == empresaId));
        // La pasajera que sí activó su cuenta la conserva sin empresa; el que nunca la activó desaparece.
        var rolPasajera = await contexto.UsuarioRoles.SingleAsync(r => r.UsuarioId == ids.PasajeraActivada);
        Assert.Null(rolPasajera.EmpresaId);
        Assert.False(await contexto.Usuarios.AnyAsync(u => u.UsuarioId == ids.PasajeroSinActivar));

        // La otra empresa sembrada no se toca.
        Assert.True(await contexto.Empresas.AnyAsync(e => e.Nombre == "EDE-B otra"));
    }

    [Fact]
    public async Task Eliminar_RespondeConflicto_CuandoHayUnaRutaEnCurso_YProhibidoAUnCoordinador()
    {
        var (empresaId, ids) = await SembrarEmpresaConOperacionAsync("EDE-C");
        using (var alcance = _fixture.CrearAlcance())
        {
            var contexto = alcance.ServiceProvider.GetRequiredService<LFMovaDbContext>();
            await contexto.Servicios.Where(s => s.Jornada!.EmpresaId == empresaId).ExecuteUpdateAsync(c => c.SetProperty(s => s.Estado, EstadoServicio.EN_CURSO));
        }

        using var administrador = _fixture.Fabrica.CreateClient();
        await AutenticarAsync(administrador, "EDE-C-ADMIN", "ClaveAdmin123");
        Assert.Equal(HttpStatusCode.Conflict, (await administrador.DeleteAsync($"/api/empresas/{empresaId}")).StatusCode);

        using var coordinador = _fixture.Fabrica.CreateClient();
        await AutenticarAsync(coordinador, "EDE-C-COORD", "ClaveCoord123");
        Assert.Equal(HttpStatusCode.Forbidden, (await coordinador.DeleteAsync($"/api/empresas/{empresaId}")).StatusCode);

        using var verificacion = _fixture.CrearAlcance();
        Assert.True(await verificacion.ServiceProvider.GetRequiredService<LFMovaDbContext>().Usuarios.AnyAsync(u => u.UsuarioId == ids.Coordinador));
    }

    /// <summary>
    /// Crea una empresa con administrador, coordinador, conductor con vehículo,
    /// una pasajera con cuenta activada y un pasajero sin activar, zona, sede,
    /// una ruta finalizada con chat, incidencia y evidencia, su registro de
    /// facturación y un cierre mensual. Crea además una segunda empresa vacía.
    /// </summary>
    private async Task<(int EmpresaId, (int Coordinador, int Conductor, int PasajeraActivada, int PasajeroSinActivar) Ids)> SembrarEmpresaConOperacionAsync(string prefijo)
    {
        using var alcance = _fixture.CrearAlcance();
        var contexto = alcance.ServiceProvider.GetRequiredService<LFMovaDbContext>();
        var hasheador = alcance.ServiceProvider.GetRequiredService<IHasheadorContrasenas>();

        var empresa = await SemillaDatosHelper.CrearEmpresaAsync(contexto, $"{prefijo} empresa");
        await SemillaDatosHelper.CrearEmpresaAsync(contexto, $"{prefijo} otra");
        await SemillaDatosHelper.CrearUsuarioConRolAsync(contexto, hasheador, $"{prefijo}-ADMIN", "ClaveAdmin123", Rol.ADMINISTRADOR_PLATAFORMA, null);
        var coordinador = await SemillaDatosHelper.CrearUsuarioConRolAsync(contexto, hasheador, $"{prefijo}-COORD", "ClaveCoord123", Rol.COORDINADOR, empresa.EmpresaId);
        var usuarioConductor = await SemillaDatosHelper.CrearUsuarioConRolAsync(contexto, hasheador, $"{prefijo}-COND", "ClaveCond123", Rol.CONDUCTOR, null);
        var pasajera = await SemillaDatosHelper.CrearUsuarioConRolAsync(contexto, hasheador, $"{prefijo}-PAS", "ClavePas123", Rol.EMPLEADO, empresa.EmpresaId);
        var sinActivar = new Usuario { Cedula = $"{prefijo}-SIN", NombreCompleto = "Sin activar", Activo = true };
        contexto.Usuarios.Add(sinActivar);
        await contexto.SaveChangesAsync();
        contexto.UsuarioRoles.Add(new UsuarioRol { UsuarioId = sinActivar.UsuarioId, Rol = Rol.EMPLEADO, EmpresaId = empresa.EmpresaId, Activo = true });

        Empleado Empleado(Usuario u) => new() { UsuarioId = u.UsuarioId, EmpresaId = empresa.EmpresaId, NombreCompleto = u.NombreCompleto, Telefono = "300", Direccion = "Calle 1", Barrio = "Centro", Activo = true };
        var empleadaActivada = Empleado(pasajera);
        var empleadoSinActivar = Empleado(sinActivar);
        var conductor = new Conductor { UsuarioId = usuarioConductor.UsuarioId, NombreCompleto = "Conductor", Telefono = "300", Activo = true };
        conductor.VinculacionesConductorEmpresa.Add(new VinculacionConductorEmpresa { EmpresaId = empresa.EmpresaId, Activa = true });
        var unidad = new UnidadOperativa { Conductor = conductor, Vehiculo = new Vehiculo { Conductor = conductor, Placa = prefijo, Marca = "Kia", Modelo = "Rio", Capacidad = 4, Activo = true }, Activa = true };
        var sede = new Sede { EmpresaId = empresa.EmpresaId, Nombre = $"{prefijo} sede", Direccion = "Calle 2", Ciudad = "Manizales", Barrio = "Centro", Activa = true };
        var jornada = new Jornada { EmpresaId = empresa.EmpresaId, FechaOperativa = new DateOnly(2025, 3, 3) };
        contexto.AddRange(empleadaActivada, empleadoSinActivar, unidad, sede, jornada);
        contexto.Zonas.Add(new Zona { EmpresaId = empresa.EmpresaId, Nombre = "Centro", Barrios = new List<string> { "Centro" }, Activa = true });
        await contexto.SaveChangesAsync();

        var fecha = new DateOnly(2025, 3, 3);
        var ruta = new Servicio { JornadaId = jornada.JornadaId, SedeId = sede.SedeId, UnidadOperativaId = unidad.UnidadOperativaId, Fecha = fecha, HoraProgramada = new TimeOnly(6, 0), Tipo = TipoServicio.ENTRADA, Estado = EstadoServicio.FINALIZADO };
        foreach (var empleado in new[] { empleadaActivada, empleadoSinActivar })
        {
            var programacion = new ProgramacionTransporte { EmpresaId = empresa.EmpresaId, Empleado = empleado, SedeId = sede.SedeId, Fecha = fecha, Hora = new TimeOnly(6, 0), Tipo = TipoServicio.ENTRADA, DireccionRecogida = "Calle 1", BarrioRecogida = "Centro" };
            var pasajero = new ServicioPasajero { Servicio = ruta, ProgramacionTransporte = programacion, Empleado = empleado, Estado = EstadoServicioPasajero.RECOGIDO, Orden = 1, DireccionRecogida = "Calle 1" };
            var conversacion = new Conversacion { ServicioPasajero = pasajero };
            conversacion.Mensajes.Add(new Mensaje { UsuarioId = usuarioConductor.UsuarioId, Contenido = "Llegué", FechaHora = DateTime.UtcNow });
            var incidencia = new Incidencia { ServicioPasajero = pasajero, Tipo = TipoIncidencia.OTRA, Descripcion = "Prueba", FechaHora = DateTime.UtcNow };
            contexto.AddRange(conversacion, new Evidencia { Incidencia = incidencia, ReferenciaArchivo = $"evidencias/{prefijo}-{empleado.UsuarioId}.jpg", FechaHora = DateTime.UtcNow });
            contexto.UbicacionesRecogidaHistorica.Add(new UbicacionRecogidaHistorica { Empleado = empleado, Direccion = "Calle 1", Barrio = "Centro", FechaRegistro = DateTime.UtcNow });
        }

        contexto.ImportacionesExcel.Add(new ImportacionExcel { EmpresaId = empresa.EmpresaId, CoordinadorId = coordinador.UsuarioId, NombreArchivo = "hoja.xlsx", FechaImportacion = DateTime.UtcNow });
        contexto.CierresMensuales.Add(new CierreMensual { EmpresaId = empresa.EmpresaId, Anio = 2025, Mes = 3, FechaCierre = DateTime.UtcNow, ConductoresActivos = 1, RutasFinalizadas = 1 });
        await contexto.SaveChangesAsync();
        contexto.UsosConductor.Add(new UsoConductor { EmpresaId = empresa.EmpresaId, ServicioId = ruta.ServicioId, Fecha = fecha, ConductorId = conductor.ConductorId, Cedula = $"{prefijo}-COND", NombreConductor = "Conductor", Placa = prefijo });
        await contexto.SaveChangesAsync();

        return (empresa.EmpresaId, (coordinador.UsuarioId, usuarioConductor.UsuarioId, pasajera.UsuarioId, sinActivar.UsuarioId));
    }

    private static async Task AutenticarAsync(HttpClient cliente, string identificador, string password)
    {
        var respuesta = await cliente.PostAsJsonAsync("/api/autenticacion/iniciar-sesion", new IniciarSesionDto { Identificador = identificador, Password = password });
        respuesta.EnsureSuccessStatusCode();
        var cuerpo = await respuesta.Content.ReadFromJsonAsync<RespuestaAutenticacionDto>();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", cuerpo!.Token);
    }
}
