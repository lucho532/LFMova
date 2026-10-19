using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using LFMova.Application.DTOs.Autenticacion;
using LFMova.Application.DTOs.Conductores;
using LFMova.Application.DTOs.Empleados;
using LFMova.Application.DTOs.Invitaciones;
using LFMova.Application.DTOs.Vehiculos;
using LFMova.Application.Interfaces;
using LFMova.Domain.Enums;
using LFMova.Infrastructure.Data;
using LFMova.IntegrationTests.Fixtures;

namespace LFMova.IntegrationTests;

/// <summary>
/// Prueba de extremo a extremo la invitación por correo a una empresa: un
/// coordinador no puede ver ni asignar roles a una persona registrada ajena a
/// su empresa hasta que la invita y la persona acepta; y una persona sin
/// cuenta se registra desde el enlace y queda directamente en la empresa.
/// </summary>
[Collection(IntegrationTestCollection.Nombre)]
public class InvitacionesTests
{
    private readonly IntegrationTestFixture _fixture;

    /// <summary>Crea la prueba con la colección compartida de PostgreSQL.</summary>
    public InvitacionesTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task PersonaRegistrada_SoloSeLeAsignaRolDespuesDeAceptarLaInvitacion()
    {
        int empresaId;
        using (var alcance = _fixture.CrearAlcance())
        {
            var contexto = alcance.ServiceProvider.GetRequiredService<LFMovaDbContext>();
            var hasheador = alcance.ServiceProvider.GetRequiredService<IHasheadorContrasenas>();
            empresaId = (await SemillaDatosHelper.CrearEmpresaAsync(contexto, "Empresa Invita")).EmpresaId;
            await SemillaDatosHelper.CrearUsuarioConRolAsync(contexto, hasheador, "INV-COORD", "ClaveCoord123", Rol.COORDINADOR, empresaId);
        }

        using var anonimo = _fixture.Fabrica.CreateClient();
        (await anonimo.PostAsJsonAsync("/api/autenticacion/registrarse", new RegistrarUsuarioDto
        {
            Correo = "inv.cond@pruebas.test", NombreCompleto = "Iván Conductor", Cedula = "INV-COND", Telefono = "3000000000", Password = "ClaveCond123"
        })).EnsureSuccessStatusCode();
        (await anonimo.PostAsJsonAsync("/api/autenticacion/confirmar-correo",
            new ConfirmarCorreoDto { Token = _fixture.Fabrica.Correo.ObtenerUltimoToken("inv.cond@pruebas.test") })).EnsureSuccessStatusCode();

        using var coordinador = _fixture.Fabrica.CreateClient();
        await AutenticarAsync(coordinador, "INV-COORD", "ClaveCoord123");

        // Antes de la invitación: no la encuentra ni puede hacerla conductor.
        Assert.Equal(HttpStatusCode.NotFound, (await coordinador.GetAsync("/api/personas/buscar?cedula=INV-COND")).StatusCode);
        var sinInvitar = await coordinador.PostAsJsonAsync($"/api/empresas/{empresaId}/conductores", ConductorConVehiculo("INV-COND", "INV111"));
        Assert.Equal(HttpStatusCode.Conflict, sinInvitar.StatusCode);

        // El coordinador escribe otro correo: la invitación llega igual al correo real de la cuenta.
        (await coordinador.PostAsJsonAsync($"/api/empresas/{empresaId}/invitaciones", new CrearInvitacionEmpresaDto
        {
            Cedula = "INV-COND", Correo = "otro.correo@pruebas.test"
        })).EnsureSuccessStatusCode();
        var token = _fixture.Fabrica.Correo.ObtenerUltimoToken("inv.cond@pruebas.test");

        var detalle = await anonimo.GetFromJsonAsync<DetalleInvitacionDto>($"/api/invitaciones/detalle?token={Uri.EscapeDataString(token)}");
        Assert.Equal("Empresa Invita", detalle!.NombreEmpresa);
        Assert.False(detalle.RequiereRegistro);

        var pendiente = Assert.Single((await coordinador.GetFromJsonAsync<List<InvitacionEmpresaDto>>($"/api/empresas/{empresaId}/invitaciones"))!);
        Assert.Equal("PENDIENTE", pendiente.Estado);
        Assert.Null(pendiente.NombreCompleto);

        // La aceptación exige sesión: sin ella no se puede.
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonimo.PostAsJsonAsync("/api/invitaciones/aceptar", new AceptarInvitacionDto { Token = token })).StatusCode);

        using var persona = _fixture.Fabrica.CreateClient();
        await AutenticarAsync(persona, "INV-COND", "ClaveCond123");
        Assert.Equal(HttpStatusCode.NoContent, (await persona.PostAsJsonAsync("/api/invitaciones/aceptar", new AceptarInvitacionDto { Token = token })).StatusCode);

        // Ya es empleado de la empresa (sin dirección) y el coordinador puede hacerla conductor.
        var empleados = await coordinador.GetFromJsonAsync<List<EmpleadoDto>>($"/api/empresas/{empresaId}/empleados");
        var empleado = Assert.Single(empleados!, e => e.Cedula == "INV-COND");
        Assert.Equal(string.Empty, empleado.Direccion);
        Assert.Equal(HttpStatusCode.OK, (await coordinador.GetAsync("/api/personas/buscar?cedula=INV-COND")).StatusCode);
        var conductor = await coordinador.PostAsJsonAsync($"/api/empresas/{empresaId}/conductores", ConductorConVehiculo("INV-COND", "INV111"));
        Assert.Equal(HttpStatusCode.Created, conductor.StatusCode);

        var aceptada = Assert.Single((await coordinador.GetFromJsonAsync<List<InvitacionEmpresaDto>>($"/api/empresas/{empresaId}/invitaciones"))!);
        Assert.Equal("ACEPTADA", aceptada.Estado);
        Assert.Equal("Iván Conductor", aceptada.NombreCompleto);
    }

    [Fact]
    public async Task PersonaSinCuenta_SeRegistraDesdeLaInvitacion_YQuedaEnLaEmpresaConElCorreoConfirmado()
    {
        int empresaId;
        using (var alcance = _fixture.CrearAlcance())
        {
            var contexto = alcance.ServiceProvider.GetRequiredService<LFMovaDbContext>();
            var hasheador = alcance.ServiceProvider.GetRequiredService<IHasheadorContrasenas>();
            empresaId = (await SemillaDatosHelper.CrearEmpresaAsync(contexto, "Empresa Invita Nuevo")).EmpresaId;
            await SemillaDatosHelper.CrearUsuarioConRolAsync(contexto, hasheador, "INV-COORD2", "ClaveCoord123", Rol.COORDINADOR, empresaId);
        }

        using var coordinador = _fixture.Fabrica.CreateClient();
        await AutenticarAsync(coordinador, "INV-COORD2", "ClaveCoord123");
        (await coordinador.PostAsJsonAsync($"/api/empresas/{empresaId}/invitaciones", new CrearInvitacionEmpresaDto
        {
            Cedula = "INV-NUEVO", Correo = "inv.nuevo@pruebas.test"
        })).EnsureSuccessStatusCode();
        var token = _fixture.Fabrica.Correo.ObtenerUltimoToken("inv.nuevo@pruebas.test");

        using var anonimo = _fixture.Fabrica.CreateClient();
        var detalle = await anonimo.GetFromJsonAsync<DetalleInvitacionDto>($"/api/invitaciones/detalle?token={Uri.EscapeDataString(token)}");
        Assert.True(detalle!.RequiereRegistro);

        // Una cédula distinta a la invitada no puede usar el enlace.
        var otraCedula = await anonimo.PostAsJsonAsync("/api/autenticacion/registrarse", new RegistrarUsuarioDto
        {
            Correo = "inv.nuevo@pruebas.test", NombreCompleto = "Nora Nueva", Cedula = "INV-OTRA", Telefono = "3000000000", Password = "ClaveNueva123", TokenInvitacion = token
        });
        Assert.Equal(HttpStatusCode.Conflict, otraCedula.StatusCode);

        var registro = await anonimo.PostAsJsonAsync("/api/autenticacion/registrarse", new RegistrarUsuarioDto
        {
            Correo = "inv.nuevo@pruebas.test", NombreCompleto = "Nora Nueva", Cedula = "INV-NUEVO", Telefono = "3000000000", Password = "ClaveNueva123", TokenInvitacion = token
        });
        registro.EnsureSuccessStatusCode();
        Assert.False((await registro.Content.ReadFromJsonAsync<ResultadoRegistroDto>())!.RequiereConfirmarCorreo);

        // Mismo correo que la invitación: puede iniciar sesión de inmediato, sin confirmar.
        using var persona = _fixture.Fabrica.CreateClient();
        await AutenticarAsync(persona, "inv.nuevo@pruebas.test", "ClaveNueva123");

        var empleados = await coordinador.GetFromJsonAsync<List<EmpleadoDto>>($"/api/empresas/{empresaId}/empleados");
        Assert.Contains(empleados!, e => e.Cedula == "INV-NUEVO");
        Assert.Equal("ACEPTADA", Assert.Single((await coordinador.GetFromJsonAsync<List<InvitacionEmpresaDto>>($"/api/empresas/{empresaId}/invitaciones"))!).Estado);
    }

    private static CrearConductorDto ConductorConVehiculo(string cedula, string placa) => new()
    {
        Cedula = cedula,
        Vehiculo = new CrearVehiculoDto
        {
            Placa = placa, Marca = "Renault", Modelo = "2023", Capacidad = 8,
            VigenciaSoat = new DateOnly(2027, 1, 1), VigenciaTecnomecanica = new DateOnly(2027, 6, 1)
        }
    };

    private static async Task AutenticarAsync(HttpClient cliente, string identificador, string password)
    {
        var respuesta = await cliente.PostAsJsonAsync("/api/autenticacion/iniciar-sesion", new IniciarSesionDto { Identificador = identificador, Password = password });
        respuesta.EnsureSuccessStatusCode();
        var cuerpo = await respuesta.Content.ReadFromJsonAsync<RespuestaAutenticacionDto>();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", cuerpo!.Token);
    }
}
