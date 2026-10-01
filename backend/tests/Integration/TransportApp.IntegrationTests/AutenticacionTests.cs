using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using TransportApp.Application.DTOs.Autenticacion;
using TransportApp.Application.DTOs.Empresas;
using TransportApp.Application.Interfaces;
using TransportApp.Domain.Entities;
using TransportApp.Domain.Enums;
using TransportApp.Infrastructure.Data;
using TransportApp.IntegrationTests.Fixtures;

namespace TransportApp.IntegrationTests;

/// <summary>
/// Prueba el flujo real de autenticación mediante HTTP: login, emisión de
/// JWT, autorización basada en roles y soporte de múltiples roles activos
/// simultáneos para un mismo usuario (ver <c>tasks.md</c> T116).
/// </summary>
[Collection(IntegrationTestCollection.Nombre)]
public class AutenticacionTests
{
    private readonly IntegrationTestFixture _fixture;

    /// <summary>Crea la prueba con la colección compartida de PostgreSQL.</summary>
    public AutenticacionTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task IniciarSesionAsync_DevuelveTokenValido_ConCredencialesCorrectas()
    {
        using var alcance = _fixture.CrearAlcance();
        var contexto = alcance.ServiceProvider.GetRequiredService<TransportAppDbContext>();
        var hasheador = alcance.ServiceProvider.GetRequiredService<IHasheadorContrasenas>();
        var empresa = await SemillaDatosHelper.CrearEmpresaAsync(contexto, "Empresa Auth 1");
        await SemillaDatosHelper.CrearUsuarioConRolAsync(contexto, hasheador, "AUTH-001", "ClaveSegura123", Rol.COORDINADOR, empresa.EmpresaId);

        using var cliente = _fixture.Fabrica.CreateClient();
        var respuesta = await cliente.PostAsJsonAsync("/api/autenticacion/iniciar-sesion", new IniciarSesionDto { Identificador = "AUTH-001", Password = "ClaveSegura123" });

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var cuerpo = await respuesta.Content.ReadFromJsonAsync<RespuestaAutenticacionDto>();
        Assert.False(string.IsNullOrWhiteSpace(cuerpo!.Token));
    }

    [Fact]
    public async Task IniciarSesionAsync_DevuelveNoAutorizado_ConContrasenaIncorrecta()
    {
        using var alcance = _fixture.CrearAlcance();
        var contexto = alcance.ServiceProvider.GetRequiredService<TransportAppDbContext>();
        var hasheador = alcance.ServiceProvider.GetRequiredService<IHasheadorContrasenas>();
        var empresa = await SemillaDatosHelper.CrearEmpresaAsync(contexto, "Empresa Auth 2");
        await SemillaDatosHelper.CrearUsuarioConRolAsync(contexto, hasheador, "AUTH-002", "ClaveSegura123", Rol.COORDINADOR, empresa.EmpresaId);

        using var cliente = _fixture.Fabrica.CreateClient();
        var respuesta = await cliente.PostAsJsonAsync("/api/autenticacion/iniciar-sesion", new IniciarSesionDto { Identificador = "AUTH-002", Password = "ClaveIncorrecta" });

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    [Fact]
    public async Task Endpoint_RequiereToken_DevuelveNoAutorizado_SinCabeceraDeAutenticacion()
    {
        using var alcance = _fixture.CrearAlcance();
        var contexto = alcance.ServiceProvider.GetRequiredService<TransportAppDbContext>();
        var empresa = await SemillaDatosHelper.CrearEmpresaAsync(contexto, "Empresa Auth 3");

        using var cliente = _fixture.Fabrica.CreateClient();
        var respuesta = await cliente.GetAsync($"/api/empresas/{empresa.EmpresaId}/sedes");

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    [Fact]
    public async Task Endpoint_PermiteAcceso_ConTokenYRolCorrectos()
    {
        using var alcance = _fixture.CrearAlcance();
        var contexto = alcance.ServiceProvider.GetRequiredService<TransportAppDbContext>();
        var hasheador = alcance.ServiceProvider.GetRequiredService<IHasheadorContrasenas>();
        var empresa = await SemillaDatosHelper.CrearEmpresaAsync(contexto, "Empresa Auth 4");
        await SemillaDatosHelper.CrearUsuarioConRolAsync(contexto, hasheador, "AUTH-004", "ClaveSegura123", Rol.COORDINADOR, empresa.EmpresaId);

        using var cliente = _fixture.Fabrica.CreateClient();
        var token = await ObtenerTokenAsync(cliente, "AUTH-004", "ClaveSegura123");
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var respuesta = await cliente.GetAsync($"/api/empresas/{empresa.EmpresaId}/sedes");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
    }

    [Fact]
    public async Task Endpoint_DevuelveProhibido_CuandoElRolNoCorrespondeAEsaEmpresa()
    {
        using var alcance = _fixture.CrearAlcance();
        var contexto = alcance.ServiceProvider.GetRequiredService<TransportAppDbContext>();
        var hasheador = alcance.ServiceProvider.GetRequiredService<IHasheadorContrasenas>();
        var empresaPropia = await SemillaDatosHelper.CrearEmpresaAsync(contexto, "Empresa Auth 5A");
        var empresaAjena = await SemillaDatosHelper.CrearEmpresaAsync(contexto, "Empresa Auth 5B");
        await SemillaDatosHelper.CrearUsuarioConRolAsync(contexto, hasheador, "AUTH-005", "ClaveSegura123", Rol.COORDINADOR, empresaPropia.EmpresaId);

        using var cliente = _fixture.Fabrica.CreateClient();
        var token = await ObtenerTokenAsync(cliente, "AUTH-005", "ClaveSegura123");
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var respuesta = await cliente.GetAsync($"/api/empresas/{empresaAjena.EmpresaId}/sedes");

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
    }

    [Fact]
    public async Task UsuarioConMultiplesRolesActivos_TieneAmbosClaimsYAmbasAutorizaciones()
    {
        using var alcance = _fixture.CrearAlcance();
        var contexto = alcance.ServiceProvider.GetRequiredService<TransportAppDbContext>();
        var hasheador = alcance.ServiceProvider.GetRequiredService<IHasheadorContrasenas>();
        var empresa = await SemillaDatosHelper.CrearEmpresaAsync(contexto, "Empresa Auth 6");
        var usuario = await SemillaDatosHelper.CrearUsuarioConRolAsync(contexto, hasheador, "AUTH-006", "ClaveSegura123", Rol.COORDINADOR, empresa.EmpresaId);
        contexto.UsuarioRoles.Add(new UsuarioRol
        {
            UsuarioId = usuario.UsuarioId,
            Rol = Rol.ADMINISTRADOR_PLATAFORMA,
            EmpresaId = null,
            Activo = true
        });
        await contexto.SaveChangesAsync();

        using var cliente = _fixture.Fabrica.CreateClient();
        var token = await ObtenerTokenAsync(cliente, "AUTH-006", "ClaveSegura123");

        var claimsDeRol = new JwtSecurityTokenHandler().ReadJwtToken(token).Claims
            .Where(c => c.Type == "rol")
            .Select(c => c.Value)
            .ToList();
        Assert.Contains($"COORDINADOR:{empresa.EmpresaId}", claimsDeRol);
        Assert.Contains("ADMINISTRADOR_PLATAFORMA", claimsDeRol);

        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Autorización que otorga el rol COORDINADOR: consultar sedes de su empresa.
        var respuestaComoCoordinador = await cliente.GetAsync($"/api/empresas/{empresa.EmpresaId}/sedes");
        Assert.Equal(HttpStatusCode.OK, respuestaComoCoordinador.StatusCode);

        // Autorización que otorga el rol ADMINISTRADOR_PLATAFORMA: crear una empresa.
        var respuestaComoAdmin = await cliente.PostAsJsonAsync("/api/empresas", new CrearEmpresaDto
        {
            Nombre = "Empresa Creada Por Admin Multirrol",
            Cif = "CIF-MULTIRROL",
            Direccion = "Calle Falsa 123",
            CedulaCoordinador = "AUTH-006-COORD",
            NombreCoordinador = "Coordinador Auth",
            TelefonoCoordinador = "3000000000",
            CorreoCoordinador = "auth006coord@pruebas.test"
        });
        Assert.Equal(HttpStatusCode.Created, respuestaComoAdmin.StatusCode);
    }

    [Fact]
    public async Task Correo_SeGuardaEnMinusculas_YElLoginNoDistingueMayusculas()
    {
        using var cliente = _fixture.Fabrica.CreateClient();

        (await cliente.PostAsJsonAsync("/api/autenticacion/registrarse", new RegistrarUsuarioDto
        {
            Correo = "  Mayus.Correo@Pruebas.TEST ",
            NombreCompleto = "Mayus Correo",
            Cedula = "AUTH-MAYUS",
            Telefono = "3001234567",
            Password = "ClaveSegura123"
        })).EnsureSuccessStatusCode();

        // El enlace de confirmación llega al correo ya normalizado.
        var token = _fixture.Fabrica.Correo.ObtenerUltimoToken("mayus.correo@pruebas.test");
        (await cliente.PostAsJsonAsync("/api/autenticacion/confirmar-correo", new ConfirmarCorreoDto { Token = token })).EnsureSuccessStatusCode();

        foreach (var identificador in new[] { "mayus.correo@pruebas.test", "MAYUS.CORREO@PRUEBAS.TEST", " Mayus.Correo@pruebas.test " })
        {
            var login = await cliente.PostAsJsonAsync("/api/autenticacion/iniciar-sesion", new IniciarSesionDto { Identificador = identificador, Password = "ClaveSegura123" });
            Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        }

        // Tampoco se puede registrar otra cuenta con el mismo correo en otras mayúsculas.
        var duplicado = await cliente.PostAsJsonAsync("/api/autenticacion/registrarse", new RegistrarUsuarioDto
        {
            Correo = "MAYUS.correo@pruebas.test", NombreCompleto = "Otra", Cedula = "AUTH-MAYUS-2", Telefono = "3001234567", Password = "ClaveSegura123"
        });
        Assert.Equal(HttpStatusCode.Conflict, duplicado.StatusCode);
    }

    [Fact]
    public async Task Registro_ConfirmacionDeCorreoYLoginPorCorreo_FuncionaDeExtremoAExtremo()
    {
        using var cliente = _fixture.Fabrica.CreateClient();

        var registro = await cliente.PostAsJsonAsync("/api/autenticacion/registrarse", new RegistrarUsuarioDto
        {
            Correo = "nuevo.empleado@pruebas.test",
            NombreCompleto = "Nuevo Empleado",
            Cedula = "AUTH-REG-1",
            Telefono = "3001234567",
            Password = "ClaveSegura123"
        });
        Assert.Equal(HttpStatusCode.OK, registro.StatusCode);
        Assert.True((await registro.Content.ReadFromJsonAsync<ResultadoRegistroDto>())!.RequiereConfirmarCorreo);

        var antesDeConfirmar = await cliente.PostAsJsonAsync("/api/autenticacion/iniciar-sesion",
            new IniciarSesionDto { Identificador = "nuevo.empleado@pruebas.test", Password = "ClaveSegura123" });
        Assert.Equal(HttpStatusCode.Unauthorized, antesDeConfirmar.StatusCode);

        var token = _fixture.Fabrica.Correo.ObtenerUltimoToken("nuevo.empleado@pruebas.test");
        var confirmacion = await cliente.PostAsJsonAsync("/api/autenticacion/confirmar-correo", new ConfirmarCorreoDto { Token = token });
        Assert.Equal(HttpStatusCode.NoContent, confirmacion.StatusCode);

        var jwt = await ObtenerTokenAsync(cliente, "nuevo.empleado@pruebas.test", "ClaveSegura123");
        var claims = new JwtSecurityTokenHandler().ReadJwtToken(jwt).Claims.ToList();
        Assert.Contains(claims, c => c.Type == "rol" && c.Value == "EMPLEADO");
        Assert.Contains(claims, c => c.Type == "nombre" && c.Value == "Nuevo Empleado");
    }

    [Fact]
    public async Task RecuperacionDeContrasena_PermiteEntrarConLaNuevaContrasena()
    {
        using var cliente = _fixture.Fabrica.CreateClient();
        await cliente.PostAsJsonAsync("/api/autenticacion/registrarse", new RegistrarUsuarioDto
        {
            Correo = "recupera@pruebas.test",
            NombreCompleto = "Persona Recupera",
            Cedula = "AUTH-REC-1",
            Telefono = "3001234567",
            Password = "ClaveVieja123"
        });
        await cliente.PostAsJsonAsync("/api/autenticacion/confirmar-correo",
            new ConfirmarCorreoDto { Token = _fixture.Fabrica.Correo.ObtenerUltimoToken("recupera@pruebas.test") });

        var solicitud = await cliente.PostAsJsonAsync("/api/autenticacion/solicitar-recuperacion", new SolicitarRecuperacionDto { Identificador = "AUTH-REC-1" });
        Assert.Equal(HttpStatusCode.NoContent, solicitud.StatusCode);

        var restablecer = await cliente.PostAsJsonAsync("/api/autenticacion/restablecer-contrasena", new RestablecerContrasenaDto
        {
            Token = _fixture.Fabrica.Correo.ObtenerUltimoToken("recupera@pruebas.test"),
            NuevaContrasena = "ClaveNueva123",
            ConfirmacionContrasena = "ClaveNueva123"
        });
        Assert.Equal(HttpStatusCode.NoContent, restablecer.StatusCode);

        var jwt = await ObtenerTokenAsync(cliente, "AUTH-REC-1", "ClaveNueva123");
        Assert.False(string.IsNullOrEmpty(jwt));
    }

    [Fact]
    public async Task Cuenta_PermiteEditarDatosYCambiarContrasena()
    {
        using var alcance = _fixture.CrearAlcance();
        var contexto = alcance.ServiceProvider.GetRequiredService<TransportAppDbContext>();
        var hasheador = alcance.ServiceProvider.GetRequiredService<IHasheadorContrasenas>();
        await SemillaDatosHelper.CrearUsuarioConRolAsync(contexto, hasheador, "AUTH-CUENTA", "ClaveSegura123", Rol.EMPLEADO, empresaId: null);

        using var cliente = _fixture.Fabrica.CreateClient();
        var token = await ObtenerTokenAsync(cliente, "AUTH-CUENTA", "ClaveSegura123");
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var actualizacion = await cliente.PutAsJsonAsync("/api/cuenta", new { nombreCompleto = "Persona Cuenta", telefono = "3005550000" });
        Assert.Equal(HttpStatusCode.OK, actualizacion.StatusCode);

        var cuenta = await cliente.GetFromJsonAsync<System.Text.Json.JsonElement>("/api/cuenta");
        Assert.Equal("Persona Cuenta", cuenta.GetProperty("nombreCompleto").GetString());

        var cambio = await cliente.PostAsJsonAsync("/api/cuenta/cambiar-contrasena",
            new { contrasenaActual = "ClaveSegura123", nuevaContrasena = "ClaveNueva456", confirmacionContrasena = "ClaveNueva456" });
        Assert.Equal(HttpStatusCode.NoContent, cambio.StatusCode);

        var nuevoToken = await ObtenerTokenAsync(cliente, "AUTH-CUENTA", "ClaveNueva456");
        Assert.False(string.IsNullOrEmpty(nuevoToken));
    }

    [Fact]
    public async Task BuscarPersona_SoloLoPermiteAAdministradoresYCoordinadores_YElCoordinadorSoloVeASuEmpresa()
    {
        using var alcance = _fixture.CrearAlcance();
        var contexto = alcance.ServiceProvider.GetRequiredService<TransportAppDbContext>();
        var hasheador = alcance.ServiceProvider.GetRequiredService<IHasheadorContrasenas>();
        var empresa = await SemillaDatosHelper.CrearEmpresaAsync(contexto, "Empresa Busqueda");
        await SemillaDatosHelper.CrearUsuarioConRolAsync(contexto, hasheador, "BUS-COORD", "ClaveSegura123", Rol.COORDINADOR, empresa.EmpresaId);
        await SemillaDatosHelper.CrearUsuarioConRolAsync(contexto, hasheador, "BUS-EMPL", "ClaveSegura123", Rol.EMPLEADO, null);

        using var comoCoordinador = _fixture.Fabrica.CreateClient();
        comoCoordinador.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await ObtenerTokenAsync(comoCoordinador, "BUS-COORD", "ClaveSegura123"));

        // Una persona registrada que no es parte de la empresa no se encuentra (igual que si no existiera).
        var ajena = await comoCoordinador.GetAsync("/api/personas/buscar?cedula=BUS-EMPL");
        Assert.Equal(HttpStatusCode.NotFound, ajena.StatusCode);
        var noExiste = await comoCoordinador.GetAsync("/api/personas/buscar?cedula=NO-EXISTE");
        Assert.Equal(HttpStatusCode.NotFound, noExiste.StatusCode);

        // Después de invitarla y que acepte, ya forma parte de la empresa y el coordinador la encuentra.
        await InvitacionesHelper.UnirAEmpresaAsync(_fixture.Fabrica, comoCoordinador, empresa.EmpresaId, "BUS-EMPL");
        var encontrada = await comoCoordinador.GetAsync("/api/personas/buscar?cedula=BUS-EMPL");
        Assert.Equal(HttpStatusCode.OK, encontrada.StatusCode);

        using var comoEmpleado = _fixture.Fabrica.CreateClient();
        comoEmpleado.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await ObtenerTokenAsync(comoEmpleado, "BUS-EMPL", "ClaveSegura123"));
        var prohibida = await comoEmpleado.GetAsync("/api/personas/buscar?cedula=BUS-COORD");
        Assert.Equal(HttpStatusCode.Forbidden, prohibida.StatusCode);
    }

    private static async Task<string> ObtenerTokenAsync(HttpClient cliente, string cedula, string contrasena)
    {
        var respuesta = await cliente.PostAsJsonAsync("/api/autenticacion/iniciar-sesion", new IniciarSesionDto { Identificador = cedula, Password = contrasena });
        respuesta.EnsureSuccessStatusCode();
        var cuerpo = await respuesta.Content.ReadFromJsonAsync<RespuestaAutenticacionDto>();
        return cuerpo!.Token;
    }
}
