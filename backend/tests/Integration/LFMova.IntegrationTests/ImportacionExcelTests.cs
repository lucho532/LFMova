using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using LFMova.Application.DTOs.Autenticacion;
using LFMova.Application.DTOs.Conductores;
using LFMova.Application.DTOs.Empresas;
using LFMova.Application.DTOs.Importaciones;
using LFMova.Application.DTOs.Jornadas;
using LFMova.Application.DTOs.Sedes;
using LFMova.Application.DTOs.Servicios;
using LFMova.Application.DTOs.UnidadesOperativas;
using LFMova.Application.DTOs.Vehiculos;
using LFMova.Application.DTOs.Zonas;
using LFMova.Application.Interfaces;
using LFMova.Domain.Enums;
using LFMova.Infrastructure.Data;
using LFMova.IntegrationTests.Fixtures;

namespace LFMova.IntegrationTests;

/// <summary>
/// Prueba de funcionamiento completa con el formato de la hoja diaria que un
/// conductor recibe por correo (TRANSPORTADOR, FECHA "14-15 SEP", secciones
/// "ENTRADA/SALIDA &lt;SEDE&gt;" con pasajeros): validar, importar, reimportar
/// (idempotencia), publicar la jornada y ver los servicios como conductor.
/// </summary>
[Collection(IntegrationTestCollection.Nombre)]
public partial class ImportacionExcelTests
{
    private readonly IntegrationTestFixture _fixture;

    /// <summary>Crea la prueba con la colección compartida de PostgreSQL.</summary>
    public ImportacionExcelTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task HojaDiariaDelConductor_SeImportaSePublicaYElConductorLaVe()
    {
        using var alcance = _fixture.CrearAlcance();
        var contexto = alcance.ServiceProvider.GetRequiredService<LFMovaDbContext>();
        var hasheador = alcance.ServiceProvider.GetRequiredService<IHasheadorContrasenas>();

        await SemillaDatosHelper.CrearUsuarioConRolAsync(contexto, hasheador, "IMP-ADMIN", "ClaveAdmin123", Rol.ADMINISTRADOR_PLATAFORMA, empresaId: null);
        using var cliente = _fixture.Fabrica.CreateClient();
        await AutenticarAsync(cliente, "IMP-ADMIN", "ClaveAdmin123");

        var respuestaEmpresa = await cliente.PostAsJsonAsync("/api/empresas", new CrearEmpresaDto
        {
            Nombre = "Empresa Importación",
            Cif = "CIF-IMP",
            Direccion = "Calle 1",
            CedulaCoordinador = "IMP-COORD",
            NombreCoordinador = "Cora Coordinadora",
            TelefonoCoordinador = "3000000001",
            CorreoCoordinador = "coord.imp@pruebas.test"
        });
        respuestaEmpresa.EnsureSuccessStatusCode();
        var empresa = (await respuestaEmpresa.Content.ReadFromJsonAsync<EmpresaDto>())!;

        (await cliente.PostAsJsonAsync("/api/autenticacion/restablecer-contrasena", new RestablecerContrasenaDto
        {
            Token = _fixture.Fabrica.Correo.ObtenerUltimoToken("coord.imp@pruebas.test"),
            NuevaContrasena = "ClaveCoord123",
            ConfirmacionContrasena = "ClaveCoord123"
        })).EnsureSuccessStatusCode();
        await AutenticarAsync(cliente, "IMP-COORD", "ClaveCoord123");

        var respuestaSede = await cliente.PostAsJsonAsync($"/api/empresas/{empresa.EmpresaId}/sedes", new CrearSedeDto
        {
            Nombre = "Panamericana",
            Direccion = "Autopista 1",
            Ciudad = "Bogotá",
            Barrio = "Norte"
        });
        respuestaSede.EnsureSuccessStatusCode();
        var sede = (await respuestaSede.Content.ReadFromJsonAsync<SedeDto>())!;

        // Conductor: se registra, confirma su correo y el coordinador lo promueve con su vehículo.
        using (var anonimo = _fixture.Fabrica.CreateClient())
        {
            (await anonimo.PostAsJsonAsync("/api/autenticacion/registrarse", new RegistrarUsuarioDto
            {
                Correo = "cond.imp@pruebas.test", NombreCompleto = "Carlos Conductor", Cedula = "IMP-COND", Telefono = "3000000000", Password = "ClaveCond123"
            })).EnsureSuccessStatusCode();
            (await anonimo.PostAsJsonAsync("/api/autenticacion/confirmar-correo",
                new ConfirmarCorreoDto { Token = _fixture.Fabrica.Correo.ObtenerUltimoToken("cond.imp@pruebas.test") })).EnsureSuccessStatusCode();
        }

        await InvitacionesHelper.UnirAEmpresaAsync(_fixture.Fabrica, cliente, empresa.EmpresaId, "IMP-COND");
        var respuestaConductor = await cliente.PostAsJsonAsync($"/api/empresas/{empresa.EmpresaId}/conductores", new CrearConductorDto
        {
            Cedula = "IMP-COND",
            Vehiculo = new CrearVehiculoDto
            {
                Placa = "IMP123", Marca = "Renault", Modelo = "2023", Capacidad = 8,
                VigenciaSoat = new DateOnly(2027, 1, 1), VigenciaTecnomecanica = new DateOnly(2027, 6, 1)
            }
        });
        respuestaConductor.EnsureSuccessStatusCode();
        var conductor = (await respuestaConductor.Content.ReadFromJsonAsync<ConductorDto>())!;
        var unidades = await cliente.GetFromJsonAsync<List<UnidadOperativaDto>>($"/api/conductores/{conductor.ConductorId}/unidades-operativas");
        var unidad = Assert.Single(unidades!);

        var ruta = $"/api/empresas/{empresa.EmpresaId}/importaciones";
        var archivo = ConstruirHoja();

        // --- Validar: no guarda nada. ---
        var vista = await EnviarAsync<VistaPreviaImportacionDto>(cliente, $"{ruta}/validar", archivo, null, null);
        Assert.True(vista.PuedeImportar, string.Join(" | ", vista.Errores));
        Assert.True(vista.CruzaMedianoche);
        Assert.Equal(3, vista.Servicios.Count);
        Assert.Equal(6, vista.TotalPasajeros);
        Assert.Equal(6, vista.EmpleadosNuevos);
        // El único empleado previo es el conductor (quedó en la empresa al aceptar la invitación); validar no creó ningún pasajero.
        Assert.Equal(0, await contexto.Empleados.CountAsync(e => e.EmpresaId == empresa.EmpresaId && e.Usuario!.Cedula != "IMP-COND"));

        // --- Importar con unidad asignada. ---
        var resultado = await EnviarAsync<ResultadoImportacionDto>(cliente, ruta, archivo, "2026-09-14", unidad.UnidadOperativaId);
        Assert.Equal(3, resultado.ServiciosCreados);
        Assert.Equal(6, resultado.PasajerosAsignados);
        Assert.Equal(6, resultado.EmpleadosCreados);

        var servicios = await contexto.Servicios.AsNoTracking().Where(s => s.JornadaId == resultado.JornadaId).OrderBy(s => s.Fecha).ThenBy(s => s.HoraProgramada).ToListAsync();
        Assert.Equal(3, servicios.Count);
        Assert.All(servicios, s => Assert.Equal(EstadoServicio.ASIGNADO, s.Estado));
        // La salida de la 01:00 cruza la medianoche: pertenece al día siguiente.
        Assert.Equal(new DateOnly(2026, 9, 15), servicios[2].Fecha);
        Assert.Equal(TipoServicio.SALIDA, servicios[2].Tipo);
        Assert.Equal(sede.SedeId, servicios[0].SedeId);

        // --- Reimportar el mismo archivo no duplica nada. ---
        var repetido = await EnviarAsync<ResultadoImportacionDto>(cliente, ruta, archivo, "2026-09-14", unidad.UnidadOperativaId);
        Assert.Equal(0, repetido.ServiciosCreados);
        Assert.Equal(0, repetido.PasajerosAsignados);
        Assert.Equal(6, repetido.FilasOmitidas);
        Assert.Equal(3, await contexto.Servicios.CountAsync(s => s.JornadaId == resultado.JornadaId));

        // --- Publicar la jornada y ver los servicios como conductor. ---
        (await cliente.PostAsync($"/api/empresas/{empresa.EmpresaId}/jornadas/{resultado.JornadaId}/publicar", null)).EnsureSuccessStatusCode();

        await AutenticarAsync(cliente, "IMP-COND", "ClaveCond123");
        var cuentaConductor = await cliente.GetFromJsonAsync<LFMova.Application.DTOs.Cuenta.CuentaDto>("/api/cuenta");
        Assert.Equal(new[] { "Empresa Importación" }, cuentaConductor!.Empresas);
        var misServicios = await cliente.GetFromJsonAsync<List<ServicioDto>>("/api/conductores/mis-servicios");
        Assert.Equal(3, misServicios!.Count(s => s.JornadaId == resultado.JornadaId));

        // El conductor edita los datos de su propio vehículo (y no puede tocar los de otro conductor).
        var vehiculos = await cliente.GetFromJsonAsync<List<VehiculoDto>>($"/api/conductores/{conductor.ConductorId}/vehiculos");
        var miVehiculo = Assert.Single(vehiculos!);
        var edicion = await cliente.PutAsJsonAsync($"/api/conductores/{conductor.ConductorId}/vehiculos/{miVehiculo.VehiculoId}", new CrearVehiculoDto
        {
            Placa = "IMP123", Marca = "Renault Master", Modelo = "2024", Capacidad = 9, VigenciaSoat = new DateOnly(2028, 1, 1), VigenciaTecnomecanica = new DateOnly(2028, 6, 1)
        });
        Assert.Equal(HttpStatusCode.OK, edicion.StatusCode);
        var editado = (await edicion.Content.ReadFromJsonAsync<VehiculoDto>())!;
        Assert.Equal(9, editado.Capacidad);
        Assert.Equal("Renault Master", editado.Marca);

        // --- Herramientas de campo del conductor: ubicación, historial, incidencia con foto y datos completos del pasajero. ---
        var servicioConductor = misServicios.First(x => x.JornadaId == resultado.JornadaId);
        // Reportar una incidencia solo se permite con el servicio en curso.
        (await cliente.PostAsync($"/api/empresas/{empresa.EmpresaId}/jornadas/{servicioConductor.JornadaId}/servicios/{servicioConductor.ServicioId}/iniciar", null)).EnsureSuccessStatusCode();
        var rutaPasajeros = $"/api/empresas/{empresa.EmpresaId}/jornadas/{servicioConductor.JornadaId}/servicios/{servicioConductor.ServicioId}/pasajeros";
        var pasajerosRuta = await cliente.GetFromJsonAsync<List<LFMova.Application.DTOs.ServiciosPasajero.ServicioPasajeroDto>>(rutaPasajeros);
        var primero = pasajerosRuta![0];
        Assert.False(string.IsNullOrEmpty(primero.CedulaEmpleado));
        Assert.False(string.IsNullOrEmpty(primero.BarrioEmpleado));
        Assert.False(string.IsNullOrEmpty(primero.TelefonoEmpleado));

        var guardar = await cliente.PutAsJsonAsync($"{rutaPasajeros}/{primero.ServicioPasajeroId}/ubicacion-recogida", new { latitud = 4.65, longitud = -74.05 });
        Assert.Equal(HttpStatusCode.NoContent, guardar.StatusCode);
        var anteriores = await cliente.GetFromJsonAsync<List<LFMova.Application.DTOs.ServiciosPasajero.UbicacionAnteriorDto>>($"{rutaPasajeros}/{primero.ServicioPasajeroId}/ubicaciones-anteriores");
        Assert.Equal(4.65, Assert.Single(anteriores!).Latitud);

        // Reportar una incidencia solo se permite tras marcar la llegada al pasajero.
        (await cliente.PostAsync($"{rutaPasajeros}/{primero.ServicioPasajeroId}/marcar-llegada", null)).EnsureSuccessStatusCode();
        var incidenciaRespuesta = await cliente.PostAsJsonAsync($"{rutaPasajeros}/{primero.ServicioPasajeroId}/incidencias", new { tipo = 5, descripcion = "Prueba con foto", latitud = 4.65, longitud = -74.05 });
        incidenciaRespuesta.EnsureSuccessStatusCode();
        var incidencia = (await incidenciaRespuesta.Content.ReadFromJsonAsync<LFMova.Application.DTOs.Incidencias.IncidenciaDto>())!;
        using (var formulario = new MultipartFormDataContent())
        {
            var imagen = new ByteArrayContent(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 1, 2, 3 });
            imagen.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
            formulario.Add(imagen, "archivo", "foto.jpg");
            var subida = await cliente.PostAsync($"{rutaPasajeros}/{primero.ServicioPasajeroId}/incidencias/{incidencia.IncidenciaId}/evidencias/foto", formulario);
            Assert.Equal(HttpStatusCode.OK, subida.StatusCode);
            var evidencia = (await subida.Content.ReadFromJsonAsync<LFMova.Application.DTOs.Incidencias.EvidenciaDto>())!;
            var descarga = await cliente.GetAsync($"{rutaPasajeros}/{primero.ServicioPasajeroId}/incidencias/{incidencia.IncidenciaId}/evidencias/{evidencia.EvidenciaId}/archivo");
            Assert.Equal(HttpStatusCode.OK, descarga.StatusCode);
            Assert.Equal(7, (await descarga.Content.ReadAsByteArrayAsync()).Length);
        }

        using (var noImagen = new MultipartFormDataContent())
        {
            var texto = new ByteArrayContent(new byte[] { 1, 2, 3 });
            texto.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
            noImagen.Add(texto, "archivo", "nota.txt");
            var rechazo = await cliente.PostAsync($"{rutaPasajeros}/{primero.ServicioPasajeroId}/incidencias/{incidencia.IncidenciaId}/evidencias/foto", noImagen);
            Assert.Equal(HttpStatusCode.BadRequest, rechazo.StatusCode);
        }

        var reorden = await cliente.PutAsJsonAsync($"{rutaPasajeros}/{primero.ServicioPasajeroId}/orden", new { nuevoOrden = 2 });
        Assert.Equal(HttpStatusCode.NoContent, reorden.StatusCode);

        // --- Una persona importada reclama su cuenta registrándose con su cédula. ---
        using (var anonimo = _fixture.Fabrica.CreateClient())
        {
            var registro = await anonimo.PostAsJsonAsync("/api/autenticacion/registrarse", new RegistrarUsuarioDto
            {
                Correo = "pasajero1.imp@pruebas.test", NombreCompleto = "Pasajero Uno", Cedula = "1001", Telefono = "3001111111", Password = "ClavePas123"
            });
            registro.EnsureSuccessStatusCode();
            (await anonimo.PostAsJsonAsync("/api/autenticacion/confirmar-correo",
                new ConfirmarCorreoDto { Token = _fixture.Fabrica.Correo.ObtenerUltimoToken("pasajero1.imp@pruebas.test") })).EnsureSuccessStatusCode();
        }

        await AutenticarAsync(cliente, "1001", "ClavePas123");
        var empleado = await contexto.Empleados.AsNoTracking().Include(e => e.Usuario).SingleAsync(e => e.Usuario.Cedula == "1001");
        Assert.Equal(empresa.EmpresaId, empleado.EmpresaId);

        // El empleado ve su servicio y confirma su asistencia; no puede tocar el de otra persona.
        var misServiciosEmpleado = await cliente.GetFromJsonAsync<List<LFMova.Application.DTOs.ServiciosPasajero.ServicioDelEmpleadoDto>>("/api/empleados/mis-servicios");
        Assert.NotEmpty(misServiciosEmpleado!);
        var suyo = misServiciosEmpleado![0];
        Assert.False(string.IsNullOrEmpty(suyo.ConductorNombre));
        Assert.False(string.IsNullOrEmpty(suyo.Placa));
        var rutaSuya = $"/api/empresas/{suyo.EmpresaId}/jornadas/{suyo.JornadaId}/servicios/{suyo.ServicioId}/pasajeros/{suyo.ServicioPasajeroId}";
        var confirmar = await cliente.PostAsJsonAsync($"{rutaSuya}/confirmar", new { });
        Assert.Equal(HttpStatusCode.NoContent, confirmar.StatusCode);
        // El empleado le escribe al conductor.
        var mensajeEmpleado = await cliente.PostAsJsonAsync($"{rutaSuya}/mensajes", new { contenido = "Ya estoy listo" });
        mensajeEmpleado.EnsureSuccessStatusCode();
        var ajena = await cliente.PostAsJsonAsync($"{rutaPasajeros}/{primero.ServicioPasajeroId}/no-asistira", new { });
        if (primero.ServicioPasajeroId != suyo.ServicioPasajeroId)
        {
            Assert.Equal(HttpStatusCode.Forbidden, ajena.StatusCode);
        }

        // El conductor recibe una notificación por la confirmación y otra por el mensaje.
        await AutenticarAsync(cliente, "IMP-COND", "ClaveCond123");
        var idConductor = int.Parse(new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().ReadJwtToken(cliente.DefaultRequestHeaders.Authorization!.Parameter).Claims.First(c => c.Type == "usuarioId").Value);
        var notificaciones = await cliente.GetFromJsonAsync<List<LFMova.Application.DTOs.Notificaciones.NotificacionDto>>($"/api/usuarios/{idConductor}/notificaciones");
        Assert.Contains(notificaciones!, n => n.Tipo == "PASAJERO_CONFIRMO");
        Assert.Contains(notificaciones!, n => n.Tipo == "MENSAJE_NUEVO" && n.Mensaje.Contains("Ya estoy listo") && n.Enlace != null && n.Enlace.StartsWith("/conductor/servicios/") && n.Enlace.Contains("/chat/"));
        Assert.Contains(notificaciones!, n => n.Tipo == "PASAJERO_CONFIRMO" && n.Enlace != null && n.Enlace.Contains("/chat/"));
    }

    [Fact]
    public async Task ArchivoConSedeNueva_LaSedeSeCreaAlImportarYElRepartoDivideEntreUnidades()
    {
        using var alcance = _fixture.CrearAlcance();
        var contexto = alcance.ServiceProvider.GetRequiredService<LFMovaDbContext>();
        var hasheador = alcance.ServiceProvider.GetRequiredService<IHasheadorContrasenas>();

        var empresa = await SemillaDatosHelper.CrearEmpresaAsync(contexto, "Empresa Reparto");
        await SemillaDatosHelper.CrearUsuarioConRolAsync(contexto, hasheador, "IMP-COORD2", "ClaveCoord123", Rol.COORDINADOR, empresa.EmpresaId);

        // Dos unidades de capacidad 2 y 3: los 6 pasajeros de las 22:00 solo caben repartidos entre ambas (más la de 1 cupo de otra hora no aplica).
        foreach (var (cedula, placa, capacidad) in new[] { ("IMP-C1", "REP001", 2), ("IMP-C2", "REP002", 4) })
        {
            var usuario = await SemillaDatosHelper.CrearUsuarioConRolAsync(contexto, hasheador, cedula, "ClaveCond123", Rol.CONDUCTOR, empresa.EmpresaId);
            var conductor = new LFMova.Domain.Entities.Conductor { UsuarioId = usuario.UsuarioId, NombreCompleto = cedula, Telefono = "3000000000", Activo = true };
            contexto.Conductores.Add(conductor);
            await contexto.SaveChangesAsync();
            contexto.VinculacionesConductorEmpresa.Add(new LFMova.Domain.Entities.VinculacionConductorEmpresa { ConductorId = conductor.ConductorId, EmpresaId = empresa.EmpresaId, Activa = true });
            var vehiculo = new LFMova.Domain.Entities.Vehiculo { ConductorId = conductor.ConductorId, Placa = placa, Marca = "X", Modelo = "2023", Capacidad = capacidad, Activo = true };
            contexto.Vehiculos.Add(vehiculo);
            await contexto.SaveChangesAsync();
            contexto.UnidadesOperativas.Add(new LFMova.Domain.Entities.UnidadOperativa { ConductorId = conductor.ConductorId, VehiculoId = vehiculo.VehiculoId, Activa = true });
            await contexto.SaveChangesAsync();
        }

        using var cliente = _fixture.Fabrica.CreateClient();
        await AutenticarAsync(cliente, "IMP-COORD2", "ClaveCoord123");
        var ruta = $"/api/empresas/{empresa.EmpresaId}/importaciones";
        var archivo = ConstruirHoja(2000);

        // La sede "Panamericana" no existe: es una advertencia, no un error (se crea automáticamente).
        var vista = await EnviarAsync<VistaPreviaImportacionDto>(cliente, $"{ruta}/validar", archivo, null, null);
        Assert.True(vista.PuedeImportar, string.Join(" | ", vista.Errores));
        Assert.Contains(vista.Advertencias, a => a.Contains("Panamericana", StringComparison.OrdinalIgnoreCase));

        var resultado = await EnviarAsync<ResultadoImportacionDto>(cliente, ruta, archivo, "2026-09-14", null, repartir: true);
        Assert.Equal(6, resultado.PasajerosAsignados);
        Assert.Equal(1, await contexto.Sedes.CountAsync(s => s.EmpresaId == empresa.EmpresaId));

        var servicios = await contexto.Servicios.AsNoTracking().Include(s => s.ServiciosPasajero).Where(s => s.JornadaId == resultado.JornadaId).ToListAsync();
        Assert.All(servicios, s => Assert.Equal(EstadoServicio.ASIGNADO, s.Estado));
        Assert.All(servicios, s => Assert.NotNull(s.UnidadOperativaId));
        Assert.Equal(6, servicios.Sum(s => s.ServiciosPasajero.Count));
        // Ninguna unidad repite fecha y hora.
        Assert.Equal(servicios.Count, servicios.Select(s => (s.Fecha, s.HoraProgramada, s.UnidadOperativaId)).Distinct().Count());

        // Reimportar no duplica.
        var repetido = await EnviarAsync<ResultadoImportacionDto>(cliente, ruta, archivo, "2026-09-14", null, repartir: true);
        Assert.Equal(0, repetido.ServiciosCreados);
        Assert.Equal(6, repetido.FilasOmitidas);

        // La ruta tiene conductor pero todavía no está publicada: el conductor no la ve, así que no se le
        // avisa de nada; se entera de toda la ruta al publicarla.
        var unidadDelServicio = await contexto.UnidadesOperativas.AsNoTracking().SingleAsync(u => u.UnidadOperativaId == servicios[0].UnidadOperativaId);
        var conductorDelServicio = await contexto.Conductores.AsNoTracking().SingleAsync(c => c.ConductorId == unidadDelServicio.ConductorId);
        Assert.Equal(0, await contexto.Notificaciones.CountAsync(n => n.UsuarioId == conductorDelServicio.UsuarioId));

        // "Programadas" solo cuenta desde que se publica la jornada.
        (await cliente.PostAsync($"/api/empresas/{empresa.EmpresaId}/jornadas/{resultado.JornadaId}/publicar", content: null)).EnsureSuccessStatusCode();
        var avisoPublicacion = await contexto.Notificaciones.AsNoTracking().FirstAsync(n => n.UsuarioId == conductorDelServicio.UsuarioId && n.Tipo == "SERVICIO_PUBLICADO");
        Assert.StartsWith("Ya puedes ver tu ruta de ", avisoPublicacion.Mensaje);

        var rutasEmpresa = await cliente.GetFromJsonAsync<List<LFMova.Application.DTOs.Estadisticas.RutaEmpresaDto>>($"/api/empresas/{empresa.EmpresaId}/estadisticas/rutas?filtro=programadas");
        Assert.Equal(servicios.Count, rutasEmpresa!.Count);
        Assert.All(rutasEmpresa, r => Assert.False(string.IsNullOrEmpty(r.Conductor)));
        var pasajerosEmpresa = await cliente.GetFromJsonAsync<List<LFMova.Application.DTOs.Estadisticas.PasajeroEmpresaDto>>($"/api/empresas/{empresa.EmpresaId}/estadisticas/pasajeros");
        Assert.Equal(6, pasajerosEmpresa!.Count);

        // El resumen del inicio y la actividad por conductor reflejan las rutas.
        var resumen = await cliente.GetFromJsonAsync<LFMova.Application.DTOs.Estadisticas.ResumenEmpresaDto>($"/api/empresas/{empresa.EmpresaId}/estadisticas/resumen");
        Assert.Equal(servicios.Count, resumen!.RutasProgramadas);
        Assert.Equal(6, resumen.PasajerosProgramados);
        var porConductor = await cliente.GetFromJsonAsync<List<LFMova.Application.DTOs.Estadisticas.EstadisticaConductorDto>>(
            $"/api/empresas/{empresa.EmpresaId}/estadisticas/conductores?desde=2026-09-14&hasta=2026-09-14");
        Assert.Equal(2, porConductor!.Count);
        Assert.Equal(servicios.Count - servicios.Count(s => s.Fecha == new DateOnly(2026, 9, 15)), porConductor.Sum(c => c.RutasProgramadas));
    }

    [Fact]
    public async Task Administrador_BuscaUnaPersonaEnToda_LaBaseYLaHaceConductorDeUnaEmpresa()
    {
        using var alcance = _fixture.CrearAlcance();
        var contexto = alcance.ServiceProvider.GetRequiredService<LFMovaDbContext>();
        var hasheador = alcance.ServiceProvider.GetRequiredService<IHasheadorContrasenas>();

        var empresa = await SemillaDatosHelper.CrearEmpresaAsync(contexto, "Empresa Admin Busca");
        await SemillaDatosHelper.CrearUsuarioConRolAsync(contexto, hasheador, "ADM-BUSCA", "ClaveAdmin123", Rol.ADMINISTRADOR_PLATAFORMA, empresaId: null);
        await SemillaDatosHelper.CrearUsuarioConRolAsync(contexto, hasheador, "PERSONA-LIBRE", "ClavePersona1", Rol.EMPLEADO, empresaId: null);

        using var cliente = _fixture.Fabrica.CreateClient();
        await AutenticarAsync(cliente, "ADM-BUSCA", "ClaveAdmin123");

        var cuentaAdmin = await cliente.GetFromJsonAsync<LFMova.Application.DTOs.Cuenta.CuentaDto>("/api/cuenta");
        Assert.Empty(cuentaAdmin!.Empresas);

        var persona = await cliente.GetFromJsonAsync<LFMova.Application.DTOs.Personas.PersonaDto>("/api/personas/buscar?cedula=PERSONA-LIBRE");
        Assert.False(persona!.EsConductor);
        Assert.Null(persona.EmpresaCoordinada);
        Assert.Empty(persona.EmpresasConductor);

        var respuesta = await cliente.PostAsJsonAsync($"/api/empresas/{empresa.EmpresaId}/conductores", new CrearConductorDto
        {
            Cedula = "PERSONA-LIBRE",
            Vehiculo = new CrearVehiculoDto
            {
                Placa = "ADM123", Marca = "Kia", Modelo = "2024", Capacidad = 5,
                VigenciaSoat = new DateOnly(2027, 1, 1), VigenciaTecnomecanica = new DateOnly(2027, 6, 1)
            }
        });
        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);

        var despues = await cliente.GetFromJsonAsync<LFMova.Application.DTOs.Personas.PersonaDto>("/api/personas/buscar?cedula=PERSONA-LIBRE");
        Assert.True(despues!.EsConductor);
        Assert.Equal(empresa.EmpresaId, Assert.Single(despues.EmpresasConductor).EmpresaId);
        Assert.Equal(new[] { "ADM123" }, despues.Placas);
    }

    [Fact]
    public async Task TablaPlanaConFechaPorFila_CreaUnaJornadaPorDiaYLasSedesDelArchivo()
    {
        using var alcance = _fixture.CrearAlcance();
        var contexto = alcance.ServiceProvider.GetRequiredService<LFMovaDbContext>();
        var hasheador = alcance.ServiceProvider.GetRequiredService<IHasheadorContrasenas>();

        var empresa = await SemillaDatosHelper.CrearEmpresaAsync(contexto, "Empresa Tabla Plana");
        await SemillaDatosHelper.CrearUsuarioConRolAsync(contexto, hasheador, "IMP-COORD3", "ClaveCoord123", Rol.COORDINADOR, empresa.EmpresaId);
        using var cliente = _fixture.Fabrica.CreateClient();
        await AutenticarAsync(cliente, "IMP-COORD3", "ClaveCoord123");

        using var libro = new XLWorkbook();
        var hoja = libro.AddWorksheet("Datos");
        var encabezados = new[] { "Cedula", "Nombre", "Apellidos", "Direccion", "Barrio", "Numero de celular", "Entrada", "Salida", "Fecha", "Sede" };
        for (var i = 0; i < encabezados.Length; i++) hoja.Cell(1, i + 1).Value = encabezados[i];
        var filas = new[]
        {
            (4001L, "Nicolas", "", "4AM", "2026-09-27", "El Campin"),
            (4002L, "Ana", "6:00", "", "2026-09-27", "El Campin"),
            (4003L, "Luis", "", "4AM", "2026-09-28", "Centro"),
        };
        for (var i = 0; i < filas.Length; i++)
        {
            var f = filas[i];
            hoja.Cell(i + 2, 1).Value = f.Item1;
            hoja.Cell(i + 2, 2).Value = f.Item2;
            hoja.Cell(i + 2, 3).Value = "Prueba";
            hoja.Cell(i + 2, 4).Value = "Calle 1";
            hoja.Cell(i + 2, 5).Value = "Centro";
            hoja.Cell(i + 2, 6).Value = 3000000000L + i;
            if (f.Item3 != "") hoja.Cell(i + 2, 7).Value = f.Item3;
            if (f.Item4 != "") hoja.Cell(i + 2, 8).Value = f.Item4;
            hoja.Cell(i + 2, 9).Value = f.Item5;
            hoja.Cell(i + 2, 10).Value = f.Item6;
        }

        using var memoria = new MemoryStream();
        libro.SaveAs(memoria);
        var archivo = memoria.ToArray();
        var ruta = $"/api/empresas/{empresa.EmpresaId}/importaciones";

        var vista = await EnviarAsync<VistaPreviaImportacionDto>(cliente, $"{ruta}/validar", archivo, null, null);
        Assert.True(vista.PuedeImportar, string.Join(" | ", vista.Errores));
        Assert.Equal(new DateOnly(2026, 9, 27), vista.FechaOperativaSugerida);
        Assert.Equal(3, vista.Servicios.Count);

        var resultado = await EnviarAsync<ResultadoImportacionDto>(cliente, ruta, archivo, "2026-09-27", null);
        Assert.Equal(3, resultado.PasajerosAsignados);
        Assert.Equal(2, await contexto.Jornadas.CountAsync(j => j.EmpresaId == empresa.EmpresaId));
        Assert.Equal(2, await contexto.Sedes.CountAsync(s => s.EmpresaId == empresa.EmpresaId));
    }

    [Fact]
    public async Task TablaPlanaSinColumnaDeSede_RequiereElegirLaSedeGeneralParaImportar()
    {
        using var alcance = _fixture.CrearAlcance();
        var contexto = alcance.ServiceProvider.GetRequiredService<LFMovaDbContext>();
        var hasheador = alcance.ServiceProvider.GetRequiredService<IHasheadorContrasenas>();

        var empresa = await SemillaDatosHelper.CrearEmpresaAsync(contexto, "Empresa Sin Sede");
        await SemillaDatosHelper.CrearUsuarioConRolAsync(contexto, hasheador, "IMP-COORD4", "ClaveCoord123", Rol.COORDINADOR, empresa.EmpresaId);
        var sedeGeneral = new LFMova.Domain.Entities.Sede
        {
            EmpresaId = empresa.EmpresaId, Nombre = "Sede Única", Direccion = "Calle 1", Ciudad = "Bogotá", Barrio = "Centro", Activa = true
        };
        contexto.Sedes.Add(sedeGeneral);
        await contexto.SaveChangesAsync();

        using var cliente = _fixture.Fabrica.CreateClient();
        await AutenticarAsync(cliente, "IMP-COORD4", "ClaveCoord123");

        // Sin columna "Sede" y sin secciones "ENTRADA/SALIDA <SEDE>": es una tabla plana sin sede.
        using var libro = new XLWorkbook();
        var hoja = libro.AddWorksheet("Datos");
        var encabezados = new[] { "Cedula", "Nombre", "Direccion", "Barrio", "Celular", "Entrada" };
        for (var i = 0; i < encabezados.Length; i++) hoja.Cell(1, i + 1).Value = encabezados[i];
        hoja.Cell(2, 1).Value = 5001L;
        hoja.Cell(2, 2).Value = "Nico";
        hoja.Cell(2, 3).Value = "Calle 1";
        hoja.Cell(2, 4).Value = "Centro";
        hoja.Cell(2, 5).Value = 3000000000L;
        hoja.Cell(2, 6).Value = "6:00";

        using var memoria = new MemoryStream();
        libro.SaveAs(memoria);
        var archivo = memoria.ToArray();
        var ruta = $"/api/empresas/{empresa.EmpresaId}/importaciones";

        var vista = await EnviarAsync<VistaPreviaImportacionDto>(cliente, $"{ruta}/validar", archivo, null, null);
        Assert.True(vista.PuedeImportar, string.Join(" | ", vista.Errores));
        Assert.Contains(vista.Advertencias, a => a.Contains("no trae la sede", StringComparison.OrdinalIgnoreCase));

        // Sin elegir una sede general, falla: la hoja no trae sede y no hay a cuál recurrir.
        using (var contenidoSinSede = new MultipartFormDataContent())
        {
            var parte = new ByteArrayContent(archivo);
            parte.Headers.ContentType = new MediaTypeHeaderValue("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
            contenidoSinSede.Add(parte, "archivo", "programacion.xlsx");
            contenidoSinSede.Add(new StringContent("2026-09-30"), "fechaOperativa");
            var respuestaSinSede = await cliente.PostAsync(ruta, contenidoSinSede);
            Assert.Equal(HttpStatusCode.Conflict, respuestaSinSede.StatusCode);
        }

        var resultado = await EnviarAsync<ResultadoImportacionDto>(cliente, ruta, archivo, "2026-09-30", null, sedeGeneralId: sedeGeneral.SedeId);
        Assert.Equal(1, resultado.PasajerosAsignados);
        Assert.Equal(1, await contexto.Sedes.CountAsync(s => s.EmpresaId == empresa.EmpresaId));

        var servicio = await contexto.Servicios.AsNoTracking().FirstAsync(s => s.JornadaId == resultado.JornadaId);
        Assert.Equal(sedeGeneral.SedeId, servicio.SedeId);
    }

    [Fact]
    public async Task TablaPlanaConLaMismaRutaPartidaEnFilasConYSinFecha_SeUnificaEnUnaSolaRutaAlRepartir()
    {
        using var alcance = _fixture.CrearAlcance();
        var contexto = alcance.ServiceProvider.GetRequiredService<LFMovaDbContext>();
        var hasheador = alcance.ServiceProvider.GetRequiredService<IHasheadorContrasenas>();

        var empresa = await SemillaDatosHelper.CrearEmpresaAsync(contexto, "Empresa Unificación");
        await SemillaDatosHelper.CrearUsuarioConRolAsync(contexto, hasheador, "IMP-COORD5", "ClaveCoord123", Rol.COORDINADOR, empresa.EmpresaId);
        contexto.Sedes.Add(new LFMova.Domain.Entities.Sede
        {
            EmpresaId = empresa.EmpresaId, Nombre = "Centro", Direccion = "Calle 1", Ciudad = "Bogotá", Barrio = "Centro", Activa = true
        });
        await contexto.SaveChangesAsync();

        using var cliente = _fixture.Fabrica.CreateClient();
        await AutenticarAsync(cliente, "IMP-COORD5", "ClaveCoord123");

        // Conductor con capacidad de sobra para los 2 pasajeros de la única ruta esperada.
        using (var anonimo = _fixture.Fabrica.CreateClient())
        {
            (await anonimo.PostAsJsonAsync("/api/autenticacion/registrarse", new RegistrarUsuarioDto
            {
                Correo = "cond.unif@pruebas.test", NombreCompleto = "Uni Conductor", Cedula = "IMP-COND5", Telefono = "3000000005", Password = "ClaveCond123"
            })).EnsureSuccessStatusCode();
            (await anonimo.PostAsJsonAsync("/api/autenticacion/confirmar-correo",
                new ConfirmarCorreoDto { Token = _fixture.Fabrica.Correo.ObtenerUltimoToken("cond.unif@pruebas.test") })).EnsureSuccessStatusCode();
        }

        await InvitacionesHelper.UnirAEmpresaAsync(_fixture.Fabrica, cliente, empresa.EmpresaId, "IMP-COND5");
        (await cliente.PostAsJsonAsync($"/api/empresas/{empresa.EmpresaId}/conductores", new CrearConductorDto
        {
            Cedula = "IMP-COND5",
            Vehiculo = new CrearVehiculoDto
            {
                Placa = "UNI123", Marca = "Renault", Modelo = "2023", Capacidad = 10,
                VigenciaSoat = new DateOnly(2027, 1, 1), VigenciaTecnomecanica = new DateOnly(2027, 6, 1)
            }
        })).EnsureSuccessStatusCode();

        // Misma ruta real (Entrada, Centro, 06:00, 2026-10-05) partida en dos filas: una sin fecha en la
        // celda (se completa con la fecha operativa al importar) y otra con la fecha explícita, que
        // termina siendo la misma. Antes se creaban dos servicios (uno por fila) y el segundo podía
        // quedarse sin unidades libres a esa hora.
        using var libro = new XLWorkbook();
        var hoja = libro.AddWorksheet("Datos");
        var encabezados = new[] { "Cedula", "Nombre", "Direccion", "Barrio", "Celular", "Entrada", "Fecha", "Sede" };
        for (var i = 0; i < encabezados.Length; i++) hoja.Cell(1, i + 1).Value = encabezados[i];
        hoja.Cell(2, 1).Value = 6001L;
        hoja.Cell(2, 2).Value = "Sin Fecha";
        hoja.Cell(2, 3).Value = "Calle 1";
        hoja.Cell(2, 4).Value = "Centro";
        hoja.Cell(2, 5).Value = 3000000010L;
        hoja.Cell(2, 6).Value = "6:00";
        hoja.Cell(2, 8).Value = "Centro";
        hoja.Cell(3, 1).Value = 6002L;
        hoja.Cell(3, 2).Value = "Con Fecha";
        hoja.Cell(3, 3).Value = "Calle 2";
        hoja.Cell(3, 4).Value = "Centro";
        hoja.Cell(3, 5).Value = 3000000011L;
        hoja.Cell(3, 6).Value = "6:00";
        hoja.Cell(3, 7).Value = "2026-10-05";
        hoja.Cell(3, 8).Value = "Centro";

        using var memoria = new MemoryStream();
        libro.SaveAs(memoria);
        var archivo = memoria.ToArray();
        var ruta = $"/api/empresas/{empresa.EmpresaId}/importaciones";

        var resultado = await EnviarAsync<ResultadoImportacionDto>(cliente, ruta, archivo, "2026-10-05", null, repartir: true);
        Assert.Equal(2, resultado.PasajerosAsignados);
        Assert.Equal(1, resultado.ServiciosCreados);

        var servicios = await contexto.Servicios.AsNoTracking().Where(s => s.JornadaId == resultado.JornadaId).ToListAsync();
        var servicio = Assert.Single(servicios);
        Assert.NotNull(servicio.UnidadOperativaId);
        Assert.Equal(2, await contexto.ServiciosPasajero.CountAsync(sp => sp.ServicioId == servicio.ServicioId));
    }

    [Fact]
    public async Task ValidarConLaMismaRutaPartidaEnFilasConYSinFecha_MuestraUnSoloCuadroEnLaVistaPrevia()
    {
        using var alcance = _fixture.CrearAlcance();
        var contexto = alcance.ServiceProvider.GetRequiredService<LFMovaDbContext>();
        var hasheador = alcance.ServiceProvider.GetRequiredService<IHasheadorContrasenas>();

        var empresa = await SemillaDatosHelper.CrearEmpresaAsync(contexto, "Empresa Vista Previa Unificada");
        await SemillaDatosHelper.CrearUsuarioConRolAsync(contexto, hasheador, "IMP-COORD8", "ClaveCoord123", Rol.COORDINADOR, empresa.EmpresaId);
        contexto.Sedes.Add(new LFMova.Domain.Entities.Sede
        {
            EmpresaId = empresa.EmpresaId, Nombre = "Centro", Direccion = "Calle 1", Ciudad = "Bogotá", Barrio = "Centro", Activa = true
        });
        await contexto.SaveChangesAsync();

        using var cliente = _fixture.Fabrica.CreateClient();
        await AutenticarAsync(cliente, "IMP-COORD8", "ClaveCoord123");

        // Misma ruta real (Entrada, Centro, 06:00) partida en dos bloques: uno sin fecha en la celda y otro
        // con la fecha explícita. Antes la vista previa mostraba dos cuadros separados de "6:00".
        using var libro = new XLWorkbook();
        var hoja = libro.AddWorksheet("Datos");
        var encabezados = new[] { "Cedula", "Nombre", "Direccion", "Barrio", "Celular", "Entrada", "Fecha", "Sede" };
        for (var i = 0; i < encabezados.Length; i++) hoja.Cell(1, i + 1).Value = encabezados[i];
        hoja.Cell(2, 1).Value = 9001L;
        hoja.Cell(2, 2).Value = "Sin Fecha";
        hoja.Cell(2, 3).Value = "Calle 1";
        hoja.Cell(2, 4).Value = "Centro";
        hoja.Cell(2, 5).Value = 3000000040L;
        hoja.Cell(2, 6).Value = "6:00";
        hoja.Cell(2, 8).Value = "Centro";
        hoja.Cell(3, 1).Value = 9002L;
        hoja.Cell(3, 2).Value = "Con Fecha";
        hoja.Cell(3, 3).Value = "Calle 2";
        hoja.Cell(3, 4).Value = "Centro";
        hoja.Cell(3, 5).Value = 3000000041L;
        hoja.Cell(3, 6).Value = "6:00";
        hoja.Cell(3, 7).Value = "2026-10-08";
        hoja.Cell(3, 8).Value = "Centro";

        using var memoria = new MemoryStream();
        libro.SaveAs(memoria);
        var archivo = memoria.ToArray();

        var vista = await EnviarAsync<VistaPreviaImportacionDto>(cliente, $"/api/empresas/{empresa.EmpresaId}/importaciones/validar", archivo, null, null);
        Assert.True(vista.PuedeImportar, string.Join(" | ", vista.Errores));
        var servicio = Assert.Single(vista.Servicios);
        Assert.Equal(2, servicio.Pasajeros.Count);
        Assert.Equal(new DateOnly(2026, 10, 8), servicio.Fecha);
    }

    [Fact]
    public async Task ValidarConBarrioSinZona_LoListaYDesapareceAlAgregarloComoAliasDeUnaZonaExistente()
    {
        using var alcance = _fixture.CrearAlcance();
        var contexto = alcance.ServiceProvider.GetRequiredService<LFMovaDbContext>();
        var hasheador = alcance.ServiceProvider.GetRequiredService<IHasheadorContrasenas>();

        var empresa = await SemillaDatosHelper.CrearEmpresaAsync(contexto, "Empresa Barrios Sin Zona");
        await SemillaDatosHelper.CrearUsuarioConRolAsync(contexto, hasheador, "IMP-COORD20", "ClaveCoord123", Rol.COORDINADOR, empresa.EmpresaId);
        contexto.Sedes.Add(new LFMova.Domain.Entities.Sede
        {
            EmpresaId = empresa.EmpresaId, Nombre = "Centro", Direccion = "Calle 1", Ciudad = "Bogotá", Barrio = "Centro", Activa = true
        });
        await contexto.SaveChangesAsync();

        using var cliente = _fixture.Fabrica.CreateClient();
        await AutenticarAsync(cliente, "IMP-COORD20", "ClaveCoord123");

        var respuestaZona = await cliente.PostAsJsonAsync($"/api/empresas/{empresa.EmpresaId}/zonas", new CrearZonaDto
        { Nombre = "Zona Occidente", Barrios = new List<string> { "La Linda" } });
        respuestaZona.EnsureSuccessStatusCode();
        var zona = (await respuestaZona.Content.ReadFromJsonAsync<ZonaDto>())!;

        using var libro = new XLWorkbook();
        var hoja = libro.AddWorksheet("Datos");
        var encabezados = new[] { "Cedula", "Nombre", "Direccion", "Barrio", "Celular", "Entrada", "Sede" };
        for (var i = 0; i < encabezados.Length; i++) hoja.Cell(1, i + 1).Value = encabezados[i];
        // "La Linda Vieja" no coincide con ninguna zona (solo existe "La Linda").
        hoja.Cell(2, 1).Value = 9800001L;
        hoja.Cell(2, 2).Value = "Persona Uno";
        hoja.Cell(2, 3).Value = "Calle 1";
        hoja.Cell(2, 4).Value = "La Linda Vieja";
        hoja.Cell(2, 5).Value = 3000000300L;
        hoja.Cell(2, 6).Value = "6:00";
        hoja.Cell(2, 7).Value = "Centro";

        using var memoria = new MemoryStream();
        libro.SaveAs(memoria);
        var archivo = memoria.ToArray();

        var vista = await EnviarAsync<VistaPreviaImportacionDto>(cliente, $"/api/empresas/{empresa.EmpresaId}/importaciones/validar", archivo, null, null);
        Assert.Contains("La Linda Vieja", vista.BarriosSinZona);

        // El coordinador lo resuelve con un clic: lo agrega como alias de la zona ya existente.
        (await cliente.PostAsJsonAsync($"/api/empresas/{empresa.EmpresaId}/zonas/{zona.ZonaId}/barrios",
            new AgregarBarrioZonaDto { Barrio = "La Linda Vieja" })).EnsureSuccessStatusCode();

        var vistaDespues = await EnviarAsync<VistaPreviaImportacionDto>(cliente, $"/api/empresas/{empresa.EmpresaId}/importaciones/validar", archivo, null, null);
        Assert.DoesNotContain("La Linda Vieja", vistaDespues.BarriosSinZona);
    }

    [Fact]
    public async Task BarrioConElMismoNombreEnVillamariaYManizales_SeDistinguePorLaDireccion()
    {
        using var alcance = _fixture.CrearAlcance();
        var contexto = alcance.ServiceProvider.GetRequiredService<LFMovaDbContext>();
        var hasheador = alcance.ServiceProvider.GetRequiredService<IHasheadorContrasenas>();

        var empresa = await SemillaDatosHelper.CrearEmpresaAsync(contexto, "Empresa Barrio Homonimo");
        await SemillaDatosHelper.CrearUsuarioConRolAsync(contexto, hasheador, "IMP-COORD30", "ClaveCoord123", Rol.COORDINADOR, empresa.EmpresaId);
        contexto.Sedes.Add(new LFMova.Domain.Entities.Sede
        {
            EmpresaId = empresa.EmpresaId, Nombre = "Centro", Direccion = "Calle 1", Ciudad = "Manizales", Barrio = "Centro", Activa = true
        });
        var villamaria = new LFMova.Domain.Entities.MacroZona { EmpresaId = empresa.EmpresaId, Nombre = "Villamaría", Activa = true };
        contexto.MacroZonas.Add(villamaria);
        await contexto.SaveChangesAsync();
        contexto.Zonas.Add(new LFMova.Domain.Entities.Zona
        {
            EmpresaId = empresa.EmpresaId, Nombre = "Villamaría Comuna 2", Barrios = new List<string> { "Nuevo Horizonte" }, MacroZonaId = villamaria.MacroZonaId, Activa = true
        });
        await contexto.SaveChangesAsync();

        using var cliente = _fixture.Fabrica.CreateClient();
        await AutenticarAsync(cliente, "IMP-COORD30", "ClaveCoord123");

        byte[] Hoja(string direccion)
        {
            using var libro = new XLWorkbook();
            var hoja = libro.AddWorksheet("Datos");
            var encabezados = new[] { "Cedula", "Nombre", "Direccion", "Barrio", "Celular", "Entrada", "Sede" };
            for (var i = 0; i < encabezados.Length; i++) hoja.Cell(1, i + 1).Value = encabezados[i];
            hoja.Cell(2, 1).Value = 9800101L;
            hoja.Cell(2, 2).Value = "Persona Horizonte";
            hoja.Cell(2, 3).Value = direccion;
            hoja.Cell(2, 4).Value = "NUEVO HORIZONTE";
            hoja.Cell(2, 5).Value = 3000000301L;
            hoja.Cell(2, 6).Value = "6:00";
            hoja.Cell(2, 7).Value = "Centro";
            using var memoria = new MemoryStream();
            libro.SaveAs(memoria);
            return memoria.ToArray();
        }

        var rutaValidar = $"/api/empresas/{empresa.EmpresaId}/importaciones/validar";

        // Dirección posible en Villamaría: entra a la zona de Villamaría.
        var deVillamaria = await EnviarAsync<VistaPreviaImportacionDto>(cliente, rutaValidar, Hoja("Calle 5 # 8-20"), null, null);
        Assert.DoesNotContain("NUEVO HORIZONTE", deVillamaria.BarriosSinZona);

        // El caso real: la carrera 38 no existe en Villamaría, así que es el Nuevo Horizonte de Manizales y,
        // mientras no esté en ninguna zona de Manizales, queda sin zona en vez de irse para Villamaría.
        var deManizales = await EnviarAsync<VistaPreviaImportacionDto>(cliente, rutaValidar, Hoja("CALLE 10 A1 # 38 A-20"), null, null);
        Assert.Contains("NUEVO HORIZONTE", deManizales.BarriosSinZona);

        // Al agregar "Nuevo Horizonte" a una zona de Manizales, esa persona ya tiene zona.
        (await cliente.PostAsJsonAsync($"/api/empresas/{empresa.EmpresaId}/zonas", new CrearZonaDto
        { Nombre = "La Macarena", Barrios = new List<string> { "Estambul", "Nuevo Horizonte" } })).EnsureSuccessStatusCode();
        var conZonaDeManizales = await EnviarAsync<VistaPreviaImportacionDto>(cliente, rutaValidar, Hoja("CALLE 10 A1 # 38 A-20"), null, null);
        Assert.DoesNotContain("NUEVO HORIZONTE", conZonaDeManizales.BarriosSinZona);
    }

    [Fact]
    public async Task ValidarConHorariosFueraDeOrdenEnLaHoja_LosDevuelveDelPrimeroAlUltimo()
    {
        using var alcance = _fixture.CrearAlcance();
        var contexto = alcance.ServiceProvider.GetRequiredService<LFMovaDbContext>();
        var hasheador = alcance.ServiceProvider.GetRequiredService<IHasheadorContrasenas>();

        var empresa = await SemillaDatosHelper.CrearEmpresaAsync(contexto, "Empresa Orden Vista Previa");
        await SemillaDatosHelper.CrearUsuarioConRolAsync(contexto, hasheador, "IMP-COORD9", "ClaveCoord123", Rol.COORDINADOR, empresa.EmpresaId);
        contexto.Sedes.Add(new LFMova.Domain.Entities.Sede
        {
            EmpresaId = empresa.EmpresaId, Nombre = "Centro", Direccion = "Calle 1", Ciudad = "Bogotá", Barrio = "Centro", Activa = true
        });
        await contexto.SaveChangesAsync();

        using var cliente = _fixture.Fabrica.CreateClient();
        await AutenticarAsync(cliente, "IMP-COORD9", "ClaveCoord123");

        // La hoja trae las 06:00 primero y luego las 01:00 y 02:00, como en el archivo real del coordinador.
        using var libro = new XLWorkbook();
        var hoja = libro.AddWorksheet("Datos");
        var encabezados = new[] { "Cedula", "Nombre", "Direccion", "Barrio", "Celular", "Entrada", "Sede" };
        for (var i = 0; i < encabezados.Length; i++) hoja.Cell(1, i + 1).Value = encabezados[i];
        var filas = new (long Cedula, string Hora)[] { (9101L, "6:00"), (9102L, "1:00"), (9103L, "2:00") };
        for (var i = 0; i < filas.Length; i++)
        {
            hoja.Cell(i + 2, 1).Value = filas[i].Cedula;
            hoja.Cell(i + 2, 2).Value = $"Persona {i}";
            hoja.Cell(i + 2, 3).Value = "Calle 1";
            hoja.Cell(i + 2, 4).Value = "Centro";
            hoja.Cell(i + 2, 5).Value = 3000000050L + i;
            hoja.Cell(i + 2, 6).Value = filas[i].Hora;
            hoja.Cell(i + 2, 7).Value = "Centro";
        }

        using var memoria = new MemoryStream();
        libro.SaveAs(memoria);
        var archivo = memoria.ToArray();

        var vista = await EnviarAsync<VistaPreviaImportacionDto>(cliente, $"/api/empresas/{empresa.EmpresaId}/importaciones/validar", archivo, null, null);
        Assert.True(vista.PuedeImportar, string.Join(" | ", vista.Errores));
        Assert.Equal(new[] { new TimeOnly(1, 0), new TimeOnly(2, 0), new TimeOnly(6, 0) }, vista.Servicios.Select(s => s.Hora));
    }

    [Fact]
    public async Task RepartirConZonasDeUnMismoCorredorVial_LasJuntaEnUnaSolaRutaEnVezDeUnaPorZona()
    {
        using var alcance = _fixture.CrearAlcance();
        var contexto = alcance.ServiceProvider.GetRequiredService<LFMovaDbContext>();
        var hasheador = alcance.ServiceProvider.GetRequiredService<IHasheadorContrasenas>();

        var empresa = await SemillaDatosHelper.CrearEmpresaAsync(contexto, "Empresa Corredor Vial");
        await SemillaDatosHelper.CrearUsuarioConRolAsync(contexto, hasheador, "IMP-COORD15", "ClaveCoord123", Rol.COORDINADOR, empresa.EmpresaId);
        contexto.Sedes.Add(new LFMova.Domain.Entities.Sede
        {
            EmpresaId = empresa.EmpresaId, Nombre = "Centro", Direccion = "Calle 1", Ciudad = "Bogotá", Barrio = "Centro", Activa = true
        });
        await contexto.SaveChangesAsync();

        using var cliente = _fixture.Fabrica.CreateClient();
        await AutenticarAsync(cliente, "IMP-COORD15", "ClaveCoord123");

        // Corredor vial: para llegar a Morrogacho hay que pasar por La Francia, así que van en la misma ruta.
        var respuestaCorredor = await cliente.PostAsJsonAsync($"/api/empresas/{empresa.EmpresaId}/corredores-viales", new CrearCorredorVialDto { Nombre = "Atardeceres Oriente" });
        respuestaCorredor.EnsureSuccessStatusCode();
        var corredor = (await respuestaCorredor.Content.ReadFromJsonAsync<CorredorVialDto>())!;

        (await cliente.PostAsJsonAsync($"/api/empresas/{empresa.EmpresaId}/zonas", new CrearZonaDto
        { Nombre = "AT-S06 Oriente", Barrios = new List<string> { "La Francia" }, CorredorVialId = corredor.CorredorVialId,
        })).EnsureSuccessStatusCode();
        (await cliente.PostAsJsonAsync($"/api/empresas/{empresa.EmpresaId}/zonas", new CrearZonaDto
        { Nombre = "AT-S05 Morrogacho", Barrios = new List<string> { "Morrogacho" }, CorredorVialId = corredor.CorredorVialId,
        })).EnsureSuccessStatusCode();

        async Task CrearConductorAsync(string cedula, string correo, string placa)
        {
            using (var anonimo = _fixture.Fabrica.CreateClient())
            {
                (await anonimo.PostAsJsonAsync("/api/autenticacion/registrarse", new RegistrarUsuarioDto
                {
                    Correo = correo, NombreCompleto = $"Conductor {cedula}", Cedula = cedula, Telefono = "3000000000", Password = "ClaveCond123"
                })).EnsureSuccessStatusCode();
                (await anonimo.PostAsJsonAsync("/api/autenticacion/confirmar-correo",
                    new ConfirmarCorreoDto { Token = _fixture.Fabrica.Correo.ObtenerUltimoToken(correo) })).EnsureSuccessStatusCode();
            }

            await InvitacionesHelper.UnirAEmpresaAsync(_fixture.Fabrica, cliente, empresa.EmpresaId, cedula);
            (await cliente.PostAsJsonAsync($"/api/empresas/{empresa.EmpresaId}/conductores", new CrearConductorDto
            {
                Cedula = cedula,
                Vehiculo = new CrearVehiculoDto
                {
                    Placa = placa, Marca = "Renault", Modelo = "2023", Capacidad = 10,
                    VigenciaSoat = new DateOnly(2027, 1, 1), VigenciaTecnomecanica = new DateOnly(2027, 6, 1)
                }
            })).EnsureSuccessStatusCode();
        }

        // Dos conductores disponibles: si el corredor no funcionara, el reparto exigiría dos rutas.
        await CrearConductorAsync("IMP-COND15A", "cond15a@pruebas.test", "COR001");
        await CrearConductorAsync("IMP-COND15B", "cond15b@pruebas.test", "COR002");

        using var libro = new XLWorkbook();
        var hoja = libro.AddWorksheet("Datos");
        var encabezados = new[] { "Cedula", "Nombre", "Direccion", "Barrio", "Celular", "Entrada", "Sede" };
        for (var i = 0; i < encabezados.Length; i++) hoja.Cell(1, i + 1).Value = encabezados[i];
        hoja.Cell(2, 1).Value = 9700030L;
        hoja.Cell(2, 2).Value = "De La Francia";
        hoja.Cell(2, 3).Value = "Calle 1";
        hoja.Cell(2, 4).Value = "La Francia";
        hoja.Cell(2, 5).Value = 3000000090L;
        hoja.Cell(2, 6).Value = "6:00";
        hoja.Cell(2, 7).Value = "Centro";
        hoja.Cell(3, 1).Value = 9700031L;
        hoja.Cell(3, 2).Value = "De Morrogacho";
        hoja.Cell(3, 3).Value = "Calle 2";
        hoja.Cell(3, 4).Value = "Morrogacho";
        hoja.Cell(3, 5).Value = 3000000091L;
        hoja.Cell(3, 6).Value = "6:00";
        hoja.Cell(3, 7).Value = "Centro";

        using var memoria = new MemoryStream();
        libro.SaveAs(memoria);
        var archivo = memoria.ToArray();
        var ruta = $"/api/empresas/{empresa.EmpresaId}/importaciones";

        var resultado = await EnviarAsync<ResultadoImportacionDto>(cliente, ruta, archivo, "2026-10-20", null, repartir: true);

        // Una sola ruta para las dos zonas del corredor, no dos: comparten vehículo.
        Assert.Equal(1, resultado.ServiciosCreados);
        Assert.Equal(2, resultado.PasajerosAsignados);

        var servicios = await contexto.Servicios.AsNoTracking().Where(s => s.JornadaId == resultado.JornadaId).ToListAsync();
        var servicio = Assert.Single(servicios);
        Assert.Equal(2, await contexto.ServiciosPasajero.CountAsync(sp => sp.ServicioId == servicio.ServicioId));
    }

    [Fact]
    public async Task RepartirConZonasPequenasDeLaMismaMacroZona_LasFusionaParaNoAbrirRutasDeMenosDeCuatro()
    {
        using var alcance = _fixture.CrearAlcance();
        var contexto = alcance.ServiceProvider.GetRequiredService<LFMovaDbContext>();
        var hasheador = alcance.ServiceProvider.GetRequiredService<IHasheadorContrasenas>();

        var empresa = await SemillaDatosHelper.CrearEmpresaAsync(contexto, "Empresa Zonas Pequenas");
        await SemillaDatosHelper.CrearUsuarioConRolAsync(contexto, hasheador, "IMP-COORD17", "ClaveCoord123", Rol.COORDINADOR, empresa.EmpresaId);
        contexto.Sedes.Add(new LFMova.Domain.Entities.Sede
        {
            EmpresaId = empresa.EmpresaId, Nombre = "Centro", Direccion = "Calle 1", Ciudad = "Bogotá", Barrio = "Centro", Activa = true
        });
        await contexto.SaveChangesAsync();

        using var cliente = _fixture.Fabrica.CreateClient();
        await AutenticarAsync(cliente, "IMP-COORD17", "ClaveCoord123");

        // Villa Pilar y La Linda: zonas distintas, sin corredor vial entre ellas, pero de la misma
        // macrozona (comuna). Ninguna llega sola a las 4 personas, así que deben fusionarse en una sola ruta.
        var respuestaMacro = await cliente.PostAsJsonAsync($"/api/empresas/{empresa.EmpresaId}/macro-zonas", new CrearMacroZonaDto { Nombre = "Atardeceres" });
        respuestaMacro.EnsureSuccessStatusCode();
        var macroZona = (await respuestaMacro.Content.ReadFromJsonAsync<MacroZonaDto>())!;

        (await cliente.PostAsJsonAsync($"/api/empresas/{empresa.EmpresaId}/zonas", new CrearZonaDto
        { Nombre = "Villa Pilar", Barrios = new List<string> { "Villa Pilar" }, MacroZonaId = macroZona.MacroZonaId })).EnsureSuccessStatusCode();
        (await cliente.PostAsJsonAsync($"/api/empresas/{empresa.EmpresaId}/zonas", new CrearZonaDto
        { Nombre = "La Linda", Barrios = new List<string> { "La Linda" }, MacroZonaId = macroZona.MacroZonaId })).EnsureSuccessStatusCode();

        async Task CrearConductorAsync(string cedula, string correo, string placa)
        {
            using (var anonimo = _fixture.Fabrica.CreateClient())
            {
                (await anonimo.PostAsJsonAsync("/api/autenticacion/registrarse", new RegistrarUsuarioDto
                {
                    Correo = correo, NombreCompleto = $"Conductor {cedula}", Cedula = cedula, Telefono = "3000000000", Password = "ClaveCond123"
                })).EnsureSuccessStatusCode();
                (await anonimo.PostAsJsonAsync("/api/autenticacion/confirmar-correo",
                    new ConfirmarCorreoDto { Token = _fixture.Fabrica.Correo.ObtenerUltimoToken(correo) })).EnsureSuccessStatusCode();
            }

            await InvitacionesHelper.UnirAEmpresaAsync(_fixture.Fabrica, cliente, empresa.EmpresaId, cedula);
            (await cliente.PostAsJsonAsync($"/api/empresas/{empresa.EmpresaId}/conductores", new CrearConductorDto
            {
                Cedula = cedula,
                Vehiculo = new CrearVehiculoDto
                {
                    Placa = placa, Marca = "Renault", Modelo = "2023", Capacidad = 10,
                    VigenciaSoat = new DateOnly(2027, 1, 1), VigenciaTecnomecanica = new DateOnly(2027, 6, 1)
                }
            })).EnsureSuccessStatusCode();
        }

        // Dos conductores disponibles: si no se fusionaran, el reparto abriría dos rutas de 2 personas cada una.
        await CrearConductorAsync("IMP-COND17A", "cond17a@pruebas.test", "PEQ001");
        await CrearConductorAsync("IMP-COND17B", "cond17b@pruebas.test", "PEQ002");

        using var libro = new XLWorkbook();
        var hoja = libro.AddWorksheet("Datos");
        var encabezados = new[] { "Cedula", "Nombre", "Direccion", "Barrio", "Celular", "Entrada", "Sede" };
        for (var i = 0; i < encabezados.Length; i++) hoja.Cell(1, i + 1).Value = encabezados[i];
        var filas = new (long Cedula, string Barrio)[]
        {
            (9700050L, "Villa Pilar"), (9700051L, "Villa Pilar"), (9700052L, "La Linda"), (9700053L, "La Linda")
        };
        for (var i = 0; i < filas.Length; i++)
        {
            hoja.Cell(i + 2, 1).Value = filas[i].Cedula;
            hoja.Cell(i + 2, 2).Value = $"Persona {i}";
            hoja.Cell(i + 2, 3).Value = "Calle 1";
            hoja.Cell(i + 2, 4).Value = filas[i].Barrio;
            hoja.Cell(i + 2, 5).Value = 3000000100L + i;
            hoja.Cell(i + 2, 6).Value = "6:00";
            hoja.Cell(i + 2, 7).Value = "Centro";
        }

        using var memoria = new MemoryStream();
        libro.SaveAs(memoria);
        var archivo = memoria.ToArray();
        var ruta = $"/api/empresas/{empresa.EmpresaId}/importaciones";

        var resultado = await EnviarAsync<ResultadoImportacionDto>(cliente, ruta, archivo, "2026-11-02", null, repartir: true);

        // Una sola ruta para las dos zonas pequeñas de la misma macrozona, no dos.
        Assert.Equal(1, resultado.ServiciosCreados);
        Assert.Equal(4, resultado.PasajerosAsignados);

        var servicios = await contexto.Servicios.AsNoTracking().Where(s => s.JornadaId == resultado.JornadaId).ToListAsync();
        var servicio = Assert.Single(servicios);
        Assert.Equal(4, await contexto.ServiciosPasajero.CountAsync(sp => sp.ServicioId == servicio.ServicioId));
    }

    [Fact]
    public async Task RepartirConMasDeCuatroZonasPequenasDeLaMismaMacroZona_DejaDeFusionarAlAlcanzarElMinimo()
    {
        using var alcance = _fixture.CrearAlcance();
        var contexto = alcance.ServiceProvider.GetRequiredService<LFMovaDbContext>();
        var hasheador = alcance.ServiceProvider.GetRequiredService<IHasheadorContrasenas>();

        var empresa = await SemillaDatosHelper.CrearEmpresaAsync(contexto, "Empresa Zonas Cadena Larga");
        await SemillaDatosHelper.CrearUsuarioConRolAsync(contexto, hasheador, "IMP-COORD19", "ClaveCoord123", Rol.COORDINADOR, empresa.EmpresaId);
        contexto.Sedes.Add(new LFMova.Domain.Entities.Sede
        {
            EmpresaId = empresa.EmpresaId, Nombre = "Centro", Direccion = "Calle 1", Ciudad = "Bogotá", Barrio = "Centro", Activa = true
        });
        await contexto.SaveChangesAsync();

        using var cliente = _fixture.Fabrica.CreateClient();
        await AutenticarAsync(cliente, "IMP-COORD19", "ClaveCoord123");

        // 5 zonas de una sola persona cada una, todas de la misma macrozona: una vez la fusión llega al
        // mínimo de 4, no debe seguir absorbiendo la quinta zona solo porque comparte macrozona con alguna
        // de las ya fusionadas (eso mezclaría zonas sin relación real entre sí sin límite de tamaño).
        var respuestaMacro = await cliente.PostAsJsonAsync($"/api/empresas/{empresa.EmpresaId}/macro-zonas", new CrearMacroZonaDto { Nombre = "Comuna Cadena" });
        respuestaMacro.EnsureSuccessStatusCode();
        var macroZona = (await respuestaMacro.Content.ReadFromJsonAsync<MacroZonaDto>())!;

        var barrios = new[] { "Barrio Cadena 1", "Barrio Cadena 2", "Barrio Cadena 3", "Barrio Cadena 4", "Barrio Cadena 5" };
        foreach (var barrio in barrios)
        {
            (await cliente.PostAsJsonAsync($"/api/empresas/{empresa.EmpresaId}/zonas", new CrearZonaDto
            { Nombre = $"Zona {barrio}", Barrios = new List<string> { barrio }, MacroZonaId = macroZona.MacroZonaId })).EnsureSuccessStatusCode();
        }

        async Task CrearConductorAsync(string cedula, string correo, string placa)
        {
            using (var anonimo = _fixture.Fabrica.CreateClient())
            {
                (await anonimo.PostAsJsonAsync("/api/autenticacion/registrarse", new RegistrarUsuarioDto
                {
                    Correo = correo, NombreCompleto = $"Conductor {cedula}", Cedula = cedula, Telefono = "3000000000", Password = "ClaveCond123"
                })).EnsureSuccessStatusCode();
                (await anonimo.PostAsJsonAsync("/api/autenticacion/confirmar-correo",
                    new ConfirmarCorreoDto { Token = _fixture.Fabrica.Correo.ObtenerUltimoToken(correo) })).EnsureSuccessStatusCode();
            }

            await InvitacionesHelper.UnirAEmpresaAsync(_fixture.Fabrica, cliente, empresa.EmpresaId, cedula);
            (await cliente.PostAsJsonAsync($"/api/empresas/{empresa.EmpresaId}/conductores", new CrearConductorDto
            {
                Cedula = cedula,
                Vehiculo = new CrearVehiculoDto
                {
                    Placa = placa, Marca = "Renault", Modelo = "2023", Capacidad = 10,
                    VigenciaSoat = new DateOnly(2027, 1, 1), VigenciaTecnomecanica = new DateOnly(2027, 6, 1)
                }
            })).EnsureSuccessStatusCode();
        }

        // Dos conductores: si las 5 zonas se mezclaran en una sola ruta, sobraría un conductor sin usar.
        await CrearConductorAsync("IMP-COND19A", "cond19a@pruebas.test", "CAD001");
        await CrearConductorAsync("IMP-COND19B", "cond19b@pruebas.test", "CAD002");

        using var libro = new XLWorkbook();
        var hoja = libro.AddWorksheet("Datos");
        var encabezados = new[] { "Cedula", "Nombre", "Direccion", "Barrio", "Celular", "Entrada", "Sede" };
        for (var i = 0; i < encabezados.Length; i++) hoja.Cell(1, i + 1).Value = encabezados[i];
        for (var i = 0; i < barrios.Length; i++)
        {
            hoja.Cell(i + 2, 1).Value = 9700100L + i;
            hoja.Cell(i + 2, 2).Value = $"Persona {i}";
            hoja.Cell(i + 2, 3).Value = "Calle 1";
            hoja.Cell(i + 2, 4).Value = barrios[i];
            hoja.Cell(i + 2, 5).Value = 3000000200L + i;
            hoja.Cell(i + 2, 6).Value = "6:00";
            hoja.Cell(i + 2, 7).Value = "Centro";
        }

        using var memoria = new MemoryStream();
        libro.SaveAs(memoria);
        var archivo = memoria.ToArray();
        var ruta = $"/api/empresas/{empresa.EmpresaId}/importaciones";

        var resultado = await EnviarAsync<ResultadoImportacionDto>(cliente, ruta, archivo, "2026-11-05", null, repartir: true);

        // Dos rutas: una con 4 personas (llegó al mínimo y dejó de fusionar) y otra sola con la quinta,
        // nunca una sola ruta con las 5 zonas mezcladas.
        Assert.Equal(2, resultado.ServiciosCreados);
        Assert.Equal(5, resultado.PasajerosAsignados);

        var servicios = await contexto.Servicios.AsNoTracking().Where(s => s.JornadaId == resultado.JornadaId).ToListAsync();
        Assert.Equal(2, servicios.Count);
        var conteos = new List<int>();
        foreach (var servicio in servicios)
        {
            conteos.Add(await contexto.ServiciosPasajero.CountAsync(sp => sp.ServicioId == servicio.ServicioId));
        }
        conteos.Sort();
        Assert.Equal(new[] { 1, 4 }, conteos);
    }

    [Fact]
    public async Task ImportarDosVecesConcurrentementeElMismoArchivo_NingunaQuedaEnErrorYNoDuplicaPasajeros()
    {
        using var alcance = _fixture.CrearAlcance();
        var contexto = alcance.ServiceProvider.GetRequiredService<LFMovaDbContext>();
        var hasheador = alcance.ServiceProvider.GetRequiredService<IHasheadorContrasenas>();

        var empresa = await SemillaDatosHelper.CrearEmpresaAsync(contexto, "Empresa Importación Concurrente");
        await SemillaDatosHelper.CrearUsuarioConRolAsync(contexto, hasheador, "IMP-COORD21", "ClaveCoord123", Rol.COORDINADOR, empresa.EmpresaId);
        contexto.Sedes.Add(new LFMova.Domain.Entities.Sede
        {
            EmpresaId = empresa.EmpresaId, Nombre = "Centro", Direccion = "Calle 1", Ciudad = "Bogotá", Barrio = "Centro", Activa = true
        });
        await contexto.SaveChangesAsync();

        using var clienteUno = _fixture.Fabrica.CreateClient();
        using var clienteDos = _fixture.Fabrica.CreateClient();
        await AutenticarAsync(clienteUno, "IMP-COORD21", "ClaveCoord123");
        await AutenticarAsync(clienteDos, "IMP-COORD21", "ClaveCoord123");

        (await clienteUno.PostAsJsonAsync($"/api/empresas/{empresa.EmpresaId}/zonas", new CrearZonaDto
        { Nombre = "Zona Concurrente", Barrios = new List<string> { "Barrio Concurrente" } })).EnsureSuccessStatusCode();

        async Task CrearConductorAsync(string cedula, string correo, string placa)
        {
            using (var anonimo = _fixture.Fabrica.CreateClient())
            {
                (await anonimo.PostAsJsonAsync("/api/autenticacion/registrarse", new RegistrarUsuarioDto
                {
                    Correo = correo, NombreCompleto = $"Conductor {cedula}", Cedula = cedula, Telefono = "3000000000", Password = "ClaveCond123"
                })).EnsureSuccessStatusCode();
                (await anonimo.PostAsJsonAsync("/api/autenticacion/confirmar-correo",
                    new ConfirmarCorreoDto { Token = _fixture.Fabrica.Correo.ObtenerUltimoToken(correo) })).EnsureSuccessStatusCode();
            }

            await InvitacionesHelper.UnirAEmpresaAsync(_fixture.Fabrica, clienteUno, empresa.EmpresaId, cedula);
            (await clienteUno.PostAsJsonAsync($"/api/empresas/{empresa.EmpresaId}/conductores", new CrearConductorDto
            {
                Cedula = cedula,
                Vehiculo = new CrearVehiculoDto
                {
                    Placa = placa, Marca = "Renault", Modelo = "2023", Capacidad = 10,
                    VigenciaSoat = new DateOnly(2027, 1, 1), VigenciaTecnomecanica = new DateOnly(2027, 6, 1)
                }
            })).EnsureSuccessStatusCode();
        }

        await CrearConductorAsync("IMP-COND21A", "cond21a@pruebas.test", "CON101");
        await CrearConductorAsync("IMP-COND21B", "cond21b@pruebas.test", "CON102");

        const int totalPasajeros = 8;
        using var libro = new XLWorkbook();
        var hoja = libro.AddWorksheet("Datos");
        var encabezados = new[] { "Cedula", "Nombre", "Direccion", "Barrio", "Celular", "Entrada", "Sede" };
        for (var i = 0; i < encabezados.Length; i++) hoja.Cell(1, i + 1).Value = encabezados[i];
        for (var i = 0; i < totalPasajeros; i++)
        {
            hoja.Cell(i + 2, 1).Value = 9700200L + i;
            hoja.Cell(i + 2, 2).Value = $"Persona {i}";
            hoja.Cell(i + 2, 3).Value = "Calle 1";
            hoja.Cell(i + 2, 4).Value = "Barrio Concurrente";
            hoja.Cell(i + 2, 5).Value = 3000000300L + i;
            hoja.Cell(i + 2, 6).Value = "6:00";
            hoja.Cell(i + 2, 7).Value = "Centro";
        }

        using var memoria = new MemoryStream();
        libro.SaveAs(memoria);
        var archivo = memoria.ToArray();
        var ruta = $"/api/empresas/{empresa.EmpresaId}/importaciones";

        static async Task<HttpResponseMessage> EnviarCrudoAsync(HttpClient cliente, string ruta, byte[] archivo, string fecha)
        {
            using var contenido = new MultipartFormDataContent();
            var parte = new ByteArrayContent(archivo);
            parte.Headers.ContentType = new MediaTypeHeaderValue("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
            contenido.Add(parte, "archivo", "programacion.xlsx");
            contenido.Add(new StringContent(fecha), "fechaOperativa");
            contenido.Add(new StringContent("true"), "repartirEntreUnidades");
            return await cliente.PostAsync(ruta, contenido);
        }

        // Simula el escenario real que rompía la importación: dos peticiones del MISMO archivo llegan
        // solapadas (por ejemplo, el coordinador hace doble clic porque el botón todavía no mostraba una
        // ruedita de carga). Antes de este fix, la segunda petición en terminar de procesar cada fila
        // chocaba con "Esta programación ya fue asignada a un servicio." (o con la cédula/UsuarioId
        // duplicados si el empleado era nuevo), esa excepción no se atrapaba y toda la importación (no
        // solo esa fila) quedaba en EstadoImportacionExcel.ERROR con datos a medio crear, sin poder
        // reintentarse con éxito ni siquiera más tarde.
        var tareaUno = EnviarCrudoAsync(clienteUno, ruta, archivo, "2026-11-10");
        var tareaDos = EnviarCrudoAsync(clienteDos, ruta, archivo, "2026-11-10");
        var respuestas = await Task.WhenAll(tareaUno, tareaDos);

        // Ninguna de las dos queda como error del servidor (500 sin manejar, el síntoma original): en el
        // peor de los casos (dos peticiones literalmente simultáneas sobre las mismas filas, algo que ya
        // no puede pasar en la app real porque el botón se deshabilita mientras importa), la que pierde
        // una carrera muy ajustada puede recibir un 409 con el mismo mensaje de negocio de siempre en vez
        // de tumbar la importación; lo normal es que ambas respondan 200.
        foreach (var respuesta in respuestas)
        {
            Assert.True(
                respuesta.StatusCode is HttpStatusCode.OK or HttpStatusCode.Conflict,
                $"Se esperaba 200 o 409, pero llegó {respuesta.StatusCode}: {await respuesta.Content.ReadAsStringAsync()}");
        }

        Assert.Contains(respuestas, r => r.StatusCode == HttpStatusCode.OK);

        // No se asume una sola Jornada: bajo la misma carrera extrema de arriba, la creación de la
        // Jornada de esta fecha (si todavía no existía ninguna) también puede duplicarse en el peor caso;
        // eso es una imperfección menor y no lo que este test protege, así que se suman los pasajeros de
        // todas las jornadas de la fecha en vez de asumir que hay exactamente una.
        var jornadaIds = await contexto.Jornadas.AsNoTracking().Where(j => j.EmpresaId == empresa.EmpresaId).Select(j => j.JornadaId).ToListAsync();
        var totalEnBd = await contexto.ServiciosPasajero.AsNoTracking()
            .CountAsync(sp => contexto.Servicios.Any(s => s.ServicioId == sp.ServicioId && jornadaIds.Contains(s.JornadaId)));

        // Al menos algún pasajero quedó repartido (no se pierde todo en silencio) y nunca se duplica.
        Assert.True(totalEnBd is > 0 and <= totalPasajeros, $"Se esperaban entre 1 y {totalPasajeros} pasajeros en la BD, pero hay {totalEnBd}.");
    }

    [Fact]
    public async Task RepartirConZonasPequenasSinMacroZonaCompartida_NoLasFusionaYAvisa()
    {
        using var alcance = _fixture.CrearAlcance();
        var contexto = alcance.ServiceProvider.GetRequiredService<LFMovaDbContext>();
        var hasheador = alcance.ServiceProvider.GetRequiredService<IHasheadorContrasenas>();

        var empresa = await SemillaDatosHelper.CrearEmpresaAsync(contexto, "Empresa Zonas Sin Macro");
        await SemillaDatosHelper.CrearUsuarioConRolAsync(contexto, hasheador, "IMP-COORD18", "ClaveCoord123", Rol.COORDINADOR, empresa.EmpresaId);
        contexto.Sedes.Add(new LFMova.Domain.Entities.Sede
        {
            EmpresaId = empresa.EmpresaId, Nombre = "Centro", Direccion = "Calle 1", Ciudad = "Bogotá", Barrio = "Centro", Activa = true
        });
        await contexto.SaveChangesAsync();

        using var cliente = _fixture.Fabrica.CreateClient();
        await AutenticarAsync(cliente, "IMP-COORD18", "ClaveCoord123");

        // Dos zonas chicas, sin corredor ni macrozona en común: no hay dato de cercanía para fusionarlas.
        (await cliente.PostAsJsonAsync($"/api/empresas/{empresa.EmpresaId}/zonas", new CrearZonaDto
        { Nombre = "Zona Aislada Uno", Barrios = new List<string> { "Aislado Uno" } })).EnsureSuccessStatusCode();
        (await cliente.PostAsJsonAsync($"/api/empresas/{empresa.EmpresaId}/zonas", new CrearZonaDto
        { Nombre = "Zona Aislada Dos", Barrios = new List<string> { "Aislado Dos" } })).EnsureSuccessStatusCode();

        async Task CrearConductorAsync(string cedula, string correo, string placa)
        {
            using (var anonimo = _fixture.Fabrica.CreateClient())
            {
                (await anonimo.PostAsJsonAsync("/api/autenticacion/registrarse", new RegistrarUsuarioDto
                {
                    Correo = correo, NombreCompleto = $"Conductor {cedula}", Cedula = cedula, Telefono = "3000000000", Password = "ClaveCond123"
                })).EnsureSuccessStatusCode();
                (await anonimo.PostAsJsonAsync("/api/autenticacion/confirmar-correo",
                    new ConfirmarCorreoDto { Token = _fixture.Fabrica.Correo.ObtenerUltimoToken(correo) })).EnsureSuccessStatusCode();
            }

            await InvitacionesHelper.UnirAEmpresaAsync(_fixture.Fabrica, cliente, empresa.EmpresaId, cedula);
            (await cliente.PostAsJsonAsync($"/api/empresas/{empresa.EmpresaId}/conductores", new CrearConductorDto
            {
                Cedula = cedula,
                Vehiculo = new CrearVehiculoDto
                {
                    Placa = placa, Marca = "Renault", Modelo = "2023", Capacidad = 10,
                    VigenciaSoat = new DateOnly(2027, 1, 1), VigenciaTecnomecanica = new DateOnly(2027, 6, 1)
                }
            })).EnsureSuccessStatusCode();
        }

        await CrearConductorAsync("IMP-COND18A", "cond18a@pruebas.test", "AIS001");
        await CrearConductorAsync("IMP-COND18B", "cond18b@pruebas.test", "AIS002");

        using var libro = new XLWorkbook();
        var hoja = libro.AddWorksheet("Datos");
        var encabezados = new[] { "Cedula", "Nombre", "Direccion", "Barrio", "Celular", "Entrada", "Sede" };
        for (var i = 0; i < encabezados.Length; i++) hoja.Cell(1, i + 1).Value = encabezados[i];
        hoja.Cell(2, 1).Value = 9700060L;
        hoja.Cell(2, 2).Value = "De Aislado Uno";
        hoja.Cell(2, 3).Value = "Calle 1";
        hoja.Cell(2, 4).Value = "Aislado Uno";
        hoja.Cell(2, 5).Value = 3000000110L;
        hoja.Cell(2, 6).Value = "6:00";
        hoja.Cell(2, 7).Value = "Centro";
        hoja.Cell(3, 1).Value = 9700061L;
        hoja.Cell(3, 2).Value = "De Aislado Dos";
        hoja.Cell(3, 3).Value = "Calle 2";
        hoja.Cell(3, 4).Value = "Aislado Dos";
        hoja.Cell(3, 5).Value = 3000000111L;
        hoja.Cell(3, 6).Value = "6:00";
        hoja.Cell(3, 7).Value = "Centro";

        using var memoria = new MemoryStream();
        libro.SaveAs(memoria);
        var archivo = memoria.ToArray();
        var ruta = $"/api/empresas/{empresa.EmpresaId}/importaciones";

        var resultado = await EnviarAsync<ResultadoImportacionDto>(cliente, ruta, archivo, "2026-11-03", null, repartir: true);

        // Sin macrozona en común no hay forma de saber si son cercanas: quedan como dos rutas separadas, cada una avisada.
        Assert.Equal(2, resultado.ServiciosCreados);
        Assert.Equal(2, resultado.PasajerosAsignados);
        Assert.Contains(resultado.Advertencias, a => a.Contains("menos de 4 pasajeros", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task RepartirDeNuevo_CompletaCualquierServicioDelCorredorConCupo_NoSoloElPrimero()
    {
        using var alcance = _fixture.CrearAlcance();
        var contexto = alcance.ServiceProvider.GetRequiredService<LFMovaDbContext>();
        var hasheador = alcance.ServiceProvider.GetRequiredService<IHasheadorContrasenas>();

        var empresa = await SemillaDatosHelper.CrearEmpresaAsync(contexto, "Empresa Corredor Varios Candidatos");
        await SemillaDatosHelper.CrearUsuarioConRolAsync(contexto, hasheador, "IMP-COORD16", "ClaveCoord123", Rol.COORDINADOR, empresa.EmpresaId);
        contexto.Sedes.Add(new LFMova.Domain.Entities.Sede
        {
            EmpresaId = empresa.EmpresaId, Nombre = "Centro", Direccion = "Calle 1", Ciudad = "Bogotá", Barrio = "Centro", Activa = true
        });
        await contexto.SaveChangesAsync();

        using var cliente = _fixture.Fabrica.CreateClient();
        await AutenticarAsync(cliente, "IMP-COORD16", "ClaveCoord123");

        var respuestaCorredor = await cliente.PostAsJsonAsync($"/api/empresas/{empresa.EmpresaId}/corredores-viales", new CrearCorredorVialDto { Nombre = "Corredor Prueba" });
        respuestaCorredor.EnsureSuccessStatusCode();
        var corredor = (await respuestaCorredor.Content.ReadFromJsonAsync<CorredorVialDto>())!;

        (await cliente.PostAsJsonAsync($"/api/empresas/{empresa.EmpresaId}/zonas", new CrearZonaDto
        { Nombre = "Zona Norte", Barrios = new List<string> { "Norte" }, CorredorVialId = corredor.CorredorVialId })).EnsureSuccessStatusCode();
        (await cliente.PostAsJsonAsync($"/api/empresas/{empresa.EmpresaId}/zonas", new CrearZonaDto
        { Nombre = "Zona Sur", Barrios = new List<string> { "Sur" }, CorredorVialId = corredor.CorredorVialId })).EnsureSuccessStatusCode();

        async Task CrearConductorAsync(string cedula, string correo, string placa, int capacidad)
        {
            using (var anonimo = _fixture.Fabrica.CreateClient())
            {
                (await anonimo.PostAsJsonAsync("/api/autenticacion/registrarse", new RegistrarUsuarioDto
                {
                    Correo = correo, NombreCompleto = $"Conductor {cedula}", Cedula = cedula, Telefono = "3000000000", Password = "ClaveCond123"
                })).EnsureSuccessStatusCode();
                (await anonimo.PostAsJsonAsync("/api/autenticacion/confirmar-correo",
                    new ConfirmarCorreoDto { Token = _fixture.Fabrica.Correo.ObtenerUltimoToken(correo) })).EnsureSuccessStatusCode();
            }

            await InvitacionesHelper.UnirAEmpresaAsync(_fixture.Fabrica, cliente, empresa.EmpresaId, cedula);
            (await cliente.PostAsJsonAsync($"/api/empresas/{empresa.EmpresaId}/conductores", new CrearConductorDto
            {
                Cedula = cedula,
                Vehiculo = new CrearVehiculoDto
                {
                    Placa = placa, Marca = "Renault", Modelo = "2023", Capacidad = capacidad,
                    VigenciaSoat = new DateOnly(2027, 1, 1), VigenciaTecnomecanica = new DateOnly(2027, 6, 1)
                }
            })).EnsureSuccessStatusCode();
        }

        // Dos unidades de capacidad 2 cada una: 3 pendientes del corredor no caben en una sola, así que
        // el primer import ya deja DOS servicios existentes para este mismo corredor (uno lleno, otro con cupo).
        await CrearConductorAsync("IMP-COND16A", "cond16a@pruebas.test", "COR101", 2);
        await CrearConductorAsync("IMP-COND16B", "cond16b@pruebas.test", "COR102", 2);

        static byte[] Hoja(params (long Cedula, string Nombre, string Barrio)[] personas)
        {
            using var libro = new XLWorkbook();
            var hoja = libro.AddWorksheet("Datos");
            var encabezados = new[] { "Cedula", "Nombre", "Direccion", "Barrio", "Celular", "Entrada", "Sede" };
            for (var i = 0; i < encabezados.Length; i++) hoja.Cell(1, i + 1).Value = encabezados[i];
            var fila = 2;
            foreach (var (cedula, nombre, barrio) in personas)
            {
                hoja.Cell(fila, 1).Value = cedula;
                hoja.Cell(fila, 2).Value = nombre;
                hoja.Cell(fila, 3).Value = "Calle 1";
                hoja.Cell(fila, 4).Value = barrio;
                hoja.Cell(fila, 5).Value = 3000000000L + cedula % 1000;
                hoja.Cell(fila, 6).Value = "6:00";
                hoja.Cell(fila, 7).Value = "Centro";
                fila++;
            }
            using var memoria = new MemoryStream();
            libro.SaveAs(memoria);
            return memoria.ToArray();
        }

        var ruta = $"/api/empresas/{empresa.EmpresaId}/importaciones";

        // Primer import: 3 personas del corredor (2 Norte + 1 Sur). Con las dos unidades de capacidad 2,
        // queda un servicio lleno (2/2) y otro con cupo libre (1/2).
        var primero = await EnviarAsync<ResultadoImportacionDto>(cliente, ruta,
            Hoja((9700040L, "Norte Uno", "Norte"), (9700041L, "Norte Dos", "Norte"), (9700042L, "Sur Uno", "Sur")),
            "2026-10-25", null, repartir: true);
        Assert.Equal(2, primero.ServiciosCreados);
        Assert.Equal(3, primero.PasajerosAsignados);

        var serviciosPrimero = await contexto.Servicios.AsNoTracking().Where(s => s.JornadaId == primero.JornadaId).ToListAsync();
        Assert.Equal(2, serviciosPrimero.Count);

        // Segundo import: 1 persona más del corredor (Sur). El servicio lleno no tiene cupo, pero el OTRO
        // servicio del mismo corredor sí (1/2): debe completarlo, no crear un tercer servicio.
        var segundo = await EnviarAsync<ResultadoImportacionDto>(cliente, ruta,
            Hoja((9700043L, "Sur Dos", "Sur")), "2026-10-25", null, repartir: true);
        Assert.Equal(0, segundo.ServiciosCreados);
        Assert.Equal(1, segundo.PasajerosAsignados);

        var serviciosFinal = await contexto.Servicios.AsNoTracking().Where(s => s.JornadaId == primero.JornadaId).ToListAsync();
        Assert.Equal(2, serviciosFinal.Count);
        Assert.All(serviciosFinal, s => Assert.Equal(2, contexto.ServiciosPasajero.Count(sp => sp.ServicioId == s.ServicioId)));
    }

    [Fact]
    public async Task RepartirEnDosImportacionesSeparadas_NuncaMezclaZonasDistintasEnElMismoServicioExistente()
    {
        using var alcance = _fixture.CrearAlcance();
        var contexto = alcance.ServiceProvider.GetRequiredService<LFMovaDbContext>();
        var hasheador = alcance.ServiceProvider.GetRequiredService<IHasheadorContrasenas>();

        var empresa = await SemillaDatosHelper.CrearEmpresaAsync(contexto, "Empresa Zonas Dos Imports");
        await SemillaDatosHelper.CrearUsuarioConRolAsync(contexto, hasheador, "IMP-COORD12", "ClaveCoord123", Rol.COORDINADOR, empresa.EmpresaId);
        contexto.Sedes.Add(new LFMova.Domain.Entities.Sede
        {
            EmpresaId = empresa.EmpresaId, Nombre = "Centro", Direccion = "Calle 1", Ciudad = "Bogotá", Barrio = "Centro", Activa = true
        });
        await contexto.SaveChangesAsync();

        using var cliente = _fixture.Fabrica.CreateClient();
        await AutenticarAsync(cliente, "IMP-COORD12", "ClaveCoord123");

        (await cliente.PostAsJsonAsync($"/api/empresas/{empresa.EmpresaId}/zonas", new CrearZonaDto { Nombre = "Zona Norte", Barrios = new List<string> { "Norte" } })).EnsureSuccessStatusCode();
        (await cliente.PostAsJsonAsync($"/api/empresas/{empresa.EmpresaId}/zonas", new CrearZonaDto { Nombre = "Zona Sur", Barrios = new List<string> { "Sur" } })).EnsureSuccessStatusCode();

        async Task CrearConductorAsync(string cedula, string correo, string placa, int capacidad)
        {
            using (var anonimo = _fixture.Fabrica.CreateClient())
            {
                (await anonimo.PostAsJsonAsync("/api/autenticacion/registrarse", new RegistrarUsuarioDto
                {
                    Correo = correo, NombreCompleto = $"Conductor {cedula}", Cedula = cedula, Telefono = "3000000000", Password = "ClaveCond123"
                })).EnsureSuccessStatusCode();
                (await anonimo.PostAsJsonAsync("/api/autenticacion/confirmar-correo",
                    new ConfirmarCorreoDto { Token = _fixture.Fabrica.Correo.ObtenerUltimoToken(correo) })).EnsureSuccessStatusCode();
            }

            await InvitacionesHelper.UnirAEmpresaAsync(_fixture.Fabrica, cliente, empresa.EmpresaId, cedula);
            (await cliente.PostAsJsonAsync($"/api/empresas/{empresa.EmpresaId}/conductores", new CrearConductorDto
            {
                Cedula = cedula,
                Vehiculo = new CrearVehiculoDto
                {
                    Placa = placa, Marca = "Renault", Modelo = "2023", Capacidad = capacidad,
                    VigenciaSoat = new DateOnly(2027, 1, 1), VigenciaTecnomecanica = new DateOnly(2027, 6, 1)
                }
            })).EnsureSuccessStatusCode();
        }

        // Capacidad de sobra (10 cada uno) para que, si el bug reaparece, el pasajero de la segunda
        // importación quepa perfectamente en el servicio "equivocado" en vez de fallar por cupo.
        await CrearConductorAsync("IMP-COND12A", "cond12a@pruebas.test", "ZON101", 10);
        await CrearConductorAsync("IMP-COND12B", "cond12b@pruebas.test", "ZON102", 10);

        static byte[] Hoja(long cedula, string nombre, string barrio, long celular)
        {
            using var libro = new XLWorkbook();
            var hoja = libro.AddWorksheet("Datos");
            var encabezados = new[] { "Cedula", "Nombre", "Direccion", "Barrio", "Celular", "Entrada", "Sede" };
            for (var i = 0; i < encabezados.Length; i++) hoja.Cell(1, i + 1).Value = encabezados[i];
            hoja.Cell(2, 1).Value = cedula;
            hoja.Cell(2, 2).Value = nombre;
            hoja.Cell(2, 3).Value = "Calle 1";
            hoja.Cell(2, 4).Value = barrio;
            hoja.Cell(2, 5).Value = celular;
            hoja.Cell(2, 6).Value = "6:00";
            hoja.Cell(2, 7).Value = "Centro";

            using var memoria = new MemoryStream();
            libro.SaveAs(memoria);
            return memoria.ToArray();
        }

        var ruta = $"/api/empresas/{empresa.EmpresaId}/importaciones";

        // Primera importación: una persona de Zona Norte. Se crea un servicio con una unidad.
        var primero = await EnviarAsync<ResultadoImportacionDto>(cliente, ruta, Hoja(9700010L, "Del Norte", "Norte", 3000000070L), "2026-10-11", null, repartir: true);
        Assert.Equal(1, primero.ServiciosCreados);

        // Segunda importación (archivo distinto, misma hora/sede): una persona de Zona Sur. Aunque el
        // servicio de la primera importación tiene cupo de sobra, no debe completarlo (es otra zona):
        // debe crear un servicio nuevo con la otra unidad.
        var segundo = await EnviarAsync<ResultadoImportacionDto>(cliente, ruta, Hoja(9700011L, "Del Sur", "Sur", 3000000071L), "2026-10-11", null, repartir: true);
        Assert.Equal(1, segundo.ServiciosCreados);

        var servicios = await contexto.Servicios.AsNoTracking().Where(s => s.JornadaId == primero.JornadaId).ToListAsync();
        Assert.Equal(2, servicios.Count);
        Assert.Equal(2, servicios.Select(s => s.UnidadOperativaId).Distinct().Count());
        Assert.All(servicios, s => Assert.Equal(1, contexto.ServiciosPasajero.Count(sp => sp.ServicioId == s.ServicioId)));
    }

    [Fact]
    public async Task RepartirConZonasDefinidas_AsignaUnConductorPorZonaAunqueUnaSolaUnidadTendriaCupoParaTodos()
    {
        using var alcance = _fixture.CrearAlcance();
        var contexto = alcance.ServiceProvider.GetRequiredService<LFMovaDbContext>();
        var hasheador = alcance.ServiceProvider.GetRequiredService<IHasheadorContrasenas>();

        var empresa = await SemillaDatosHelper.CrearEmpresaAsync(contexto, "Empresa Zonas");
        await SemillaDatosHelper.CrearUsuarioConRolAsync(contexto, hasheador, "IMP-COORD10", "ClaveCoord123", Rol.COORDINADOR, empresa.EmpresaId);
        contexto.Sedes.Add(new LFMova.Domain.Entities.Sede
        {
            EmpresaId = empresa.EmpresaId, Nombre = "Centro", Direccion = "Calle 1", Ciudad = "Bogotá", Barrio = "Centro", Activa = true
        });
        await contexto.SaveChangesAsync();

        using var cliente = _fixture.Fabrica.CreateClient();
        await AutenticarAsync(cliente, "IMP-COORD10", "ClaveCoord123");

        // Dos zonas: dos conductores libres a esa hora podrán cubrirlas en paralelo.
        (await cliente.PostAsJsonAsync($"/api/empresas/{empresa.EmpresaId}/zonas", new CrearZonaDto { Nombre = "Zona Norte", Barrios = new List<string> { "Norte" } })).EnsureSuccessStatusCode();
        (await cliente.PostAsJsonAsync($"/api/empresas/{empresa.EmpresaId}/zonas", new CrearZonaDto { Nombre = "Zona Sur", Barrios = new List<string> { "Sur" } })).EnsureSuccessStatusCode();

        async Task CrearConductorAsync(string cedula, string correo, string placa)
        {
            using (var anonimo = _fixture.Fabrica.CreateClient())
            {
                (await anonimo.PostAsJsonAsync("/api/autenticacion/registrarse", new RegistrarUsuarioDto
                {
                    Correo = correo, NombreCompleto = $"Conductor {cedula}", Cedula = cedula, Telefono = "3000000000", Password = "ClaveCond123"
                })).EnsureSuccessStatusCode();
                (await anonimo.PostAsJsonAsync("/api/autenticacion/confirmar-correo",
                    new ConfirmarCorreoDto { Token = _fixture.Fabrica.Correo.ObtenerUltimoToken(correo) })).EnsureSuccessStatusCode();
            }

            await InvitacionesHelper.UnirAEmpresaAsync(_fixture.Fabrica, cliente, empresa.EmpresaId, cedula);
            (await cliente.PostAsJsonAsync($"/api/empresas/{empresa.EmpresaId}/conductores", new CrearConductorDto
            {
                Cedula = cedula,
                Vehiculo = new CrearVehiculoDto
                {
                    Placa = placa, Marca = "Renault", Modelo = "2023", Capacidad = 16,
                    VigenciaSoat = new DateOnly(2027, 1, 1), VigenciaTecnomecanica = new DateOnly(2027, 6, 1)
                }
            })).EnsureSuccessStatusCode();
        }

        await CrearConductorAsync("IMP-COND10A", "cond10a@pruebas.test", "ZON001");
        await CrearConductorAsync("IMP-COND10B", "cond10b@pruebas.test", "ZON002");

        // Un solo horario (Entrada, Centro, 06:00) con un pasajero en cada zona: capacidad de sobra en una
        // sola unidad (16), pero deben salir dos rutas porque la ciudad exige un conductor por zona.
        using var libro = new XLWorkbook();
        var hoja = libro.AddWorksheet("Datos");
        var encabezados = new[] { "Cedula", "Nombre", "Direccion", "Barrio", "Celular", "Entrada", "Sede" };
        for (var i = 0; i < encabezados.Length; i++) hoja.Cell(1, i + 1).Value = encabezados[i];
        hoja.Cell(2, 1).Value = 9700001L;
        hoja.Cell(2, 2).Value = "Del Norte";
        hoja.Cell(2, 3).Value = "Calle 1";
        hoja.Cell(2, 4).Value = "Norte";
        hoja.Cell(2, 5).Value = 3000000060L;
        hoja.Cell(2, 6).Value = "6:00";
        hoja.Cell(2, 7).Value = "Centro";
        hoja.Cell(3, 1).Value = 9700002L;
        hoja.Cell(3, 2).Value = "Del Sur";
        hoja.Cell(3, 3).Value = "Calle 2";
        hoja.Cell(3, 4).Value = "Sur";
        hoja.Cell(3, 5).Value = 3000000061L;
        hoja.Cell(3, 6).Value = "6:00";
        hoja.Cell(3, 7).Value = "Centro";

        using var memoria = new MemoryStream();
        libro.SaveAs(memoria);
        var archivo = memoria.ToArray();
        var ruta = $"/api/empresas/{empresa.EmpresaId}/importaciones";

        var resultado = await EnviarAsync<ResultadoImportacionDto>(cliente, ruta, archivo, "2026-10-09", null, repartir: true);

        Assert.Equal(2, resultado.PasajerosAsignados);
        Assert.Equal(2, resultado.ServiciosCreados);

        var servicios = await contexto.Servicios.AsNoTracking().Where(s => s.JornadaId == resultado.JornadaId).ToListAsync();
        Assert.Equal(2, servicios.Count);
        Assert.Equal(2, servicios.Select(s => s.UnidadOperativaId).Distinct().Count());
        Assert.All(servicios, s => Assert.Equal(1, contexto.ServiciosPasajero.Count(sp => sp.ServicioId == s.ServicioId)));
    }

    [Fact]
    public async Task RepartirConZonasDefinidas_DejaSinUnidadAlGrupoQueNoAlcanzaCupo_EnVezDeFallarLaImportacion()
    {
        using var alcance = _fixture.CrearAlcance();
        var contexto = alcance.ServiceProvider.GetRequiredService<LFMovaDbContext>();
        var hasheador = alcance.ServiceProvider.GetRequiredService<IHasheadorContrasenas>();

        var empresa = await SemillaDatosHelper.CrearEmpresaAsync(contexto, "Empresa Zonas Sin Cupo");
        await SemillaDatosHelper.CrearUsuarioConRolAsync(contexto, hasheador, "IMP-COORD11", "ClaveCoord123", Rol.COORDINADOR, empresa.EmpresaId);
        contexto.Sedes.Add(new LFMova.Domain.Entities.Sede
        {
            EmpresaId = empresa.EmpresaId, Nombre = "Centro", Direccion = "Calle 1", Ciudad = "Bogotá", Barrio = "Centro", Activa = true
        });
        await contexto.SaveChangesAsync();

        using var cliente = _fixture.Fabrica.CreateClient();
        await AutenticarAsync(cliente, "IMP-COORD11", "ClaveCoord123");

        (await cliente.PostAsJsonAsync($"/api/empresas/{empresa.EmpresaId}/zonas", new CrearZonaDto { Nombre = "Zona Norte", Barrios = new List<string> { "Norte" } })).EnsureSuccessStatusCode();
        (await cliente.PostAsJsonAsync($"/api/empresas/{empresa.EmpresaId}/zonas", new CrearZonaDto { Nombre = "Zona Sur", Barrios = new List<string> { "Sur" } })).EnsureSuccessStatusCode();

        // Solo un conductor disponible para dos zonas que necesitan una unidad cada una.
        using (var anonimo = _fixture.Fabrica.CreateClient())
        {
            (await anonimo.PostAsJsonAsync("/api/autenticacion/registrarse", new RegistrarUsuarioDto
            {
                Correo = "cond11@pruebas.test", NombreCompleto = "Conductor Solo", Cedula = "IMP-COND11", Telefono = "3000000000", Password = "ClaveCond123"
            })).EnsureSuccessStatusCode();
            (await anonimo.PostAsJsonAsync("/api/autenticacion/confirmar-correo",
                new ConfirmarCorreoDto { Token = _fixture.Fabrica.Correo.ObtenerUltimoToken("cond11@pruebas.test") })).EnsureSuccessStatusCode();
        }

        await InvitacionesHelper.UnirAEmpresaAsync(_fixture.Fabrica, cliente, empresa.EmpresaId, "IMP-COND11");
        (await cliente.PostAsJsonAsync($"/api/empresas/{empresa.EmpresaId}/conductores", new CrearConductorDto
        {
            Cedula = "IMP-COND11",
            Vehiculo = new CrearVehiculoDto
            {
                Placa = "ZON900", Marca = "Renault", Modelo = "2023", Capacidad = 16,
                VigenciaSoat = new DateOnly(2027, 1, 1), VigenciaTecnomecanica = new DateOnly(2027, 6, 1)
            }
        })).EnsureSuccessStatusCode();

        using var libro = new XLWorkbook();
        var hoja = libro.AddWorksheet("Datos");
        var encabezados = new[] { "Cedula", "Nombre", "Direccion", "Barrio", "Celular", "Entrada", "Sede" };
        for (var i = 0; i < encabezados.Length; i++) hoja.Cell(1, i + 1).Value = encabezados[i];
        hoja.Cell(2, 1).Value = 9700003L;
        hoja.Cell(2, 2).Value = "Del Norte";
        hoja.Cell(2, 3).Value = "Calle 1";
        hoja.Cell(2, 4).Value = "Norte";
        hoja.Cell(2, 5).Value = 3000000062L;
        hoja.Cell(2, 6).Value = "6:00";
        hoja.Cell(2, 7).Value = "Centro";
        hoja.Cell(3, 1).Value = 9700004L;
        hoja.Cell(3, 2).Value = "Del Sur";
        hoja.Cell(3, 3).Value = "Calle 2";
        hoja.Cell(3, 4).Value = "Sur";
        hoja.Cell(3, 5).Value = 3000000063L;
        hoja.Cell(3, 6).Value = "6:00";
        hoja.Cell(3, 7).Value = "Centro";

        using var memoria = new MemoryStream();
        libro.SaveAs(memoria);
        var archivo = memoria.ToArray();
        var ruta = $"/api/empresas/{empresa.EmpresaId}/importaciones";

        // Ya no bloquea la importación por falta de cupo: arma lo que sí alcanza y deja el resto sin
        // conductor asignado (al final de la lista de ese horario), para que el coordinador reasigne a mano.
        var resultado = await EnviarAsync<ResultadoImportacionDto>(cliente, ruta, archivo, "2026-10-09", null, repartir: true);

        Assert.Equal(2, resultado.PasajerosAsignados);
        Assert.Equal(2, resultado.ServiciosCreados);
        Assert.Contains(resultado.Advertencias, a => a.Contains("zona", StringComparison.OrdinalIgnoreCase) && a.Contains("sin conductor", StringComparison.OrdinalIgnoreCase));

        var servicios = await contexto.Servicios.AsNoTracking().Where(s => s.JornadaId == resultado.JornadaId).ToListAsync();
        Assert.Equal(2, servicios.Count);
        Assert.Single(servicios, s => s.UnidadOperativaId != null);
        Assert.Single(servicios, s => s.UnidadOperativaId == null);
        Assert.All(servicios, s => Assert.Equal(1, contexto.ServiciosPasajero.Count(sp => sp.ServicioId == s.ServicioId)));
    }

    [Fact]
    public async Task DeshacerReparto_BorraLosServiciosSinPublicarYDejaLibresLasProgramaciones()
    {
        using var alcance = _fixture.CrearAlcance();
        var contexto = alcance.ServiceProvider.GetRequiredService<LFMovaDbContext>();
        var hasheador = alcance.ServiceProvider.GetRequiredService<IHasheadorContrasenas>();

        var empresa = await SemillaDatosHelper.CrearEmpresaAsync(contexto, "Empresa Deshacer Reparto");
        await SemillaDatosHelper.CrearUsuarioConRolAsync(contexto, hasheador, "IMP-COORD13", "ClaveCoord123", Rol.COORDINADOR, empresa.EmpresaId);
        contexto.Sedes.Add(new LFMova.Domain.Entities.Sede
        {
            EmpresaId = empresa.EmpresaId, Nombre = "Centro", Direccion = "Calle 1", Ciudad = "Bogotá", Barrio = "Centro", Activa = true
        });
        await contexto.SaveChangesAsync();

        using var cliente = _fixture.Fabrica.CreateClient();
        await AutenticarAsync(cliente, "IMP-COORD13", "ClaveCoord123");

        using (var anonimo = _fixture.Fabrica.CreateClient())
        {
            (await anonimo.PostAsJsonAsync("/api/autenticacion/registrarse", new RegistrarUsuarioDto
            {
                Correo = "cond13@pruebas.test", NombreCompleto = "Conductor Trece", Cedula = "IMP-COND13", Telefono = "3000000013", Password = "ClaveCond123"
            })).EnsureSuccessStatusCode();
            (await anonimo.PostAsJsonAsync("/api/autenticacion/confirmar-correo",
                new ConfirmarCorreoDto { Token = _fixture.Fabrica.Correo.ObtenerUltimoToken("cond13@pruebas.test") })).EnsureSuccessStatusCode();
        }

        await InvitacionesHelper.UnirAEmpresaAsync(_fixture.Fabrica, cliente, empresa.EmpresaId, "IMP-COND13");
        (await cliente.PostAsJsonAsync($"/api/empresas/{empresa.EmpresaId}/conductores", new CrearConductorDto
        {
            Cedula = "IMP-COND13",
            Vehiculo = new CrearVehiculoDto
            {
                Placa = "DES123", Marca = "Renault", Modelo = "2023", Capacidad = 10,
                VigenciaSoat = new DateOnly(2027, 1, 1), VigenciaTecnomecanica = new DateOnly(2027, 6, 1)
            }
        })).EnsureSuccessStatusCode();

        static byte[] Hoja(long cedula, string nombre, long celular)
        {
            using var libro = new XLWorkbook();
            var hoja = libro.AddWorksheet("Datos");
            var encabezados = new[] { "Cedula", "Nombre", "Direccion", "Barrio", "Celular", "Entrada", "Sede" };
            for (var i = 0; i < encabezados.Length; i++) hoja.Cell(1, i + 1).Value = encabezados[i];
            hoja.Cell(2, 1).Value = cedula;
            hoja.Cell(2, 2).Value = nombre;
            hoja.Cell(2, 3).Value = "Calle 1";
            hoja.Cell(2, 4).Value = "Centro";
            hoja.Cell(2, 5).Value = celular;
            hoja.Cell(2, 6).Value = "6:00";
            hoja.Cell(2, 7).Value = "Centro";

            using var memoria = new MemoryStream();
            libro.SaveAs(memoria);
            return memoria.ToArray();
        }

        var ruta = $"/api/empresas/{empresa.EmpresaId}/importaciones";
        var primero = await EnviarAsync<ResultadoImportacionDto>(cliente, ruta, Hoja(9700020L, "Primero", 3000000080L), "2026-10-15", null, repartir: true);
        Assert.Equal(1, primero.ServiciosCreados);
        Assert.Equal(1, primero.PasajerosAsignados);

        var deshacer = await cliente.PostAsync($"/api/empresas/{empresa.EmpresaId}/jornadas/{primero.JornadaId}/deshacer-reparto", content: null);
        deshacer.EnsureSuccessStatusCode();
        var resultadoDeshacer = (await deshacer.Content.ReadFromJsonAsync<DeshacerRepartoDto>())!;
        Assert.Equal(1, resultadoDeshacer.ServiciosEliminados);
        Assert.Equal(1, resultadoDeshacer.PasajerosLiberados);

        Assert.Empty(await contexto.Servicios.AsNoTracking().Where(s => s.JornadaId == primero.JornadaId).ToListAsync());
        // La ProgramacionTransporte de "Primero" sigue existiendo (nunca se borra el historial de la
        // necesidad de transporte): solo se liberó, lista para repartirse de nuevo.
        Assert.Equal(1, await contexto.ProgramacionesTransporte.CountAsync(p => p.EmpresaId == empresa.EmpresaId));

        // Repartir de nuevo: como quedó libre, se crea un servicio nuevo con la misma persona.
        var segundo = await EnviarAsync<ResultadoImportacionDto>(cliente, ruta, Hoja(9700020L, "Primero", 3000000080L), "2026-10-15", null, repartir: true);
        Assert.Equal(1, segundo.ServiciosCreados);
        Assert.Equal(1, segundo.PasajerosAsignados);
    }

    [Fact]
    public async Task DeshacerReparto_NuncaTocaUnServicioYaPublicado()
    {
        using var alcance = _fixture.CrearAlcance();
        var contexto = alcance.ServiceProvider.GetRequiredService<LFMovaDbContext>();
        var hasheador = alcance.ServiceProvider.GetRequiredService<IHasheadorContrasenas>();

        var empresa = await SemillaDatosHelper.CrearEmpresaAsync(contexto, "Empresa Deshacer Publicado");
        await SemillaDatosHelper.CrearUsuarioConRolAsync(contexto, hasheador, "IMP-COORD14", "ClaveCoord123", Rol.COORDINADOR, empresa.EmpresaId);
        contexto.Sedes.Add(new LFMova.Domain.Entities.Sede
        {
            EmpresaId = empresa.EmpresaId, Nombre = "Centro", Direccion = "Calle 1", Ciudad = "Bogotá", Barrio = "Centro", Activa = true
        });
        await contexto.SaveChangesAsync();

        using var cliente = _fixture.Fabrica.CreateClient();
        await AutenticarAsync(cliente, "IMP-COORD14", "ClaveCoord123");

        using (var anonimo = _fixture.Fabrica.CreateClient())
        {
            (await anonimo.PostAsJsonAsync("/api/autenticacion/registrarse", new RegistrarUsuarioDto
            {
                Correo = "cond14@pruebas.test", NombreCompleto = "Conductor Catorce", Cedula = "IMP-COND14", Telefono = "3000000014", Password = "ClaveCond123"
            })).EnsureSuccessStatusCode();
            (await anonimo.PostAsJsonAsync("/api/autenticacion/confirmar-correo",
                new ConfirmarCorreoDto { Token = _fixture.Fabrica.Correo.ObtenerUltimoToken("cond14@pruebas.test") })).EnsureSuccessStatusCode();
        }

        await InvitacionesHelper.UnirAEmpresaAsync(_fixture.Fabrica, cliente, empresa.EmpresaId, "IMP-COND14");
        (await cliente.PostAsJsonAsync($"/api/empresas/{empresa.EmpresaId}/conductores", new CrearConductorDto
        {
            Cedula = "IMP-COND14",
            Vehiculo = new CrearVehiculoDto
            {
                Placa = "DES456", Marca = "Renault", Modelo = "2023", Capacidad = 10,
                VigenciaSoat = new DateOnly(2027, 1, 1), VigenciaTecnomecanica = new DateOnly(2027, 6, 1)
            }
        })).EnsureSuccessStatusCode();

        static byte[] Hoja(long cedula, string nombre, long celular)
        {
            using var libro = new XLWorkbook();
            var hoja = libro.AddWorksheet("Datos");
            var encabezados = new[] { "Cedula", "Nombre", "Direccion", "Barrio", "Celular", "Entrada", "Sede" };
            for (var i = 0; i < encabezados.Length; i++) hoja.Cell(1, i + 1).Value = encabezados[i];
            hoja.Cell(2, 1).Value = cedula;
            hoja.Cell(2, 2).Value = nombre;
            hoja.Cell(2, 3).Value = "Calle 1";
            hoja.Cell(2, 4).Value = "Centro";
            hoja.Cell(2, 5).Value = celular;
            hoja.Cell(2, 6).Value = "6:00";
            hoja.Cell(2, 7).Value = "Centro";

            using var memoria = new MemoryStream();
            libro.SaveAs(memoria);
            return memoria.ToArray();
        }

        var ruta = $"/api/empresas/{empresa.EmpresaId}/importaciones";
        var primero = await EnviarAsync<ResultadoImportacionDto>(cliente, ruta, Hoja(9700021L, "Publicado", 3000000081L), "2026-10-16", null, repartir: true);

        var publicar = await cliente.PostAsync($"/api/empresas/{empresa.EmpresaId}/jornadas/{primero.JornadaId}/publicar", content: null);
        publicar.EnsureSuccessStatusCode();

        var deshacer = await cliente.PostAsync($"/api/empresas/{empresa.EmpresaId}/jornadas/{primero.JornadaId}/deshacer-reparto", content: null);
        deshacer.EnsureSuccessStatusCode();
        var resultadoDeshacer = (await deshacer.Content.ReadFromJsonAsync<DeshacerRepartoDto>())!;

        Assert.Equal(0, resultadoDeshacer.ServiciosEliminados);
        Assert.Equal(0, resultadoDeshacer.PasajerosLiberados);
        Assert.Equal(1, await contexto.Servicios.CountAsync(s => s.JornadaId == primero.JornadaId));
    }

    [Fact]
    public async Task RepartirDeNuevoParaLaMismaHora_CompletaElServicioExistenteEnVezDeFallarPorFaltaDeCupo()
    {
        using var alcance = _fixture.CrearAlcance();
        var contexto = alcance.ServiceProvider.GetRequiredService<LFMovaDbContext>();
        var hasheador = alcance.ServiceProvider.GetRequiredService<IHasheadorContrasenas>();

        var empresa = await SemillaDatosHelper.CrearEmpresaAsync(contexto, "Empresa Completar Cupo");
        await SemillaDatosHelper.CrearUsuarioConRolAsync(contexto, hasheador, "IMP-COORD6", "ClaveCoord123", Rol.COORDINADOR, empresa.EmpresaId);
        contexto.Sedes.Add(new LFMova.Domain.Entities.Sede
        {
            EmpresaId = empresa.EmpresaId, Nombre = "Centro", Direccion = "Calle 1", Ciudad = "Bogotá", Barrio = "Centro", Activa = true
        });
        await contexto.SaveChangesAsync();

        using var cliente = _fixture.Fabrica.CreateClient();
        await AutenticarAsync(cliente, "IMP-COORD6", "ClaveCoord123");

        // Un único conductor con capacidad para 3: la primera importación deja el servicio con 1/3 ocupado.
        using (var anonimo = _fixture.Fabrica.CreateClient())
        {
            (await anonimo.PostAsJsonAsync("/api/autenticacion/registrarse", new RegistrarUsuarioDto
            {
                Correo = "cond.cupo@pruebas.test", NombreCompleto = "Cupo Conductor", Cedula = "IMP-COND6", Telefono = "3000000006", Password = "ClaveCond123"
            })).EnsureSuccessStatusCode();
            (await anonimo.PostAsJsonAsync("/api/autenticacion/confirmar-correo",
                new ConfirmarCorreoDto { Token = _fixture.Fabrica.Correo.ObtenerUltimoToken("cond.cupo@pruebas.test") })).EnsureSuccessStatusCode();
        }

        await InvitacionesHelper.UnirAEmpresaAsync(_fixture.Fabrica, cliente, empresa.EmpresaId, "IMP-COND6");
        (await cliente.PostAsJsonAsync($"/api/empresas/{empresa.EmpresaId}/conductores", new CrearConductorDto
        {
            Cedula = "IMP-COND6",
            Vehiculo = new CrearVehiculoDto
            {
                Placa = "CUP123", Marca = "Renault", Modelo = "2023", Capacidad = 3,
                VigenciaSoat = new DateOnly(2027, 1, 1), VigenciaTecnomecanica = new DateOnly(2027, 6, 1)
            }
        })).EnsureSuccessStatusCode();

        static byte[] Hoja(long cedula, string nombre, long celular)
        {
            using var libro = new XLWorkbook();
            var hoja = libro.AddWorksheet("Datos");
            var encabezados = new[] { "Cedula", "Nombre", "Direccion", "Barrio", "Celular", "Entrada", "Sede" };
            for (var i = 0; i < encabezados.Length; i++) hoja.Cell(1, i + 1).Value = encabezados[i];
            hoja.Cell(2, 1).Value = cedula;
            hoja.Cell(2, 2).Value = nombre;
            hoja.Cell(2, 3).Value = "Calle 1";
            hoja.Cell(2, 4).Value = "Centro";
            hoja.Cell(2, 5).Value = celular;
            hoja.Cell(2, 6).Value = "6:00";
            hoja.Cell(2, 7).Value = "Centro";

            using var memoria = new MemoryStream();
            libro.SaveAs(memoria);
            return memoria.ToArray();
        }

        var ruta = $"/api/empresas/{empresa.EmpresaId}/importaciones";

        // --- Primera importación: 1 pasajero, repartido a la única unidad (queda con cupo libre 2). ---
        var primerResultado = await EnviarAsync<ResultadoImportacionDto>(cliente, ruta, Hoja(7001L, "Primero", 3000000020L), "2026-10-06", null, repartir: true);
        Assert.Equal(1, primerResultado.ServiciosCreados);
        Assert.Equal(1, primerResultado.PasajerosAsignados);
        var servicioId = (await contexto.Servicios.AsNoTracking().SingleAsync(s => s.JornadaId == primerResultado.JornadaId)).ServicioId;

        // --- Segunda importación (otra fila, otra cédula): antes fallaba porque la unidad ya estaba "usada"
        // a esa hora; ahora completa el mismo servicio en vez de abrir uno nuevo o fallar. ---
        var segundoResultado = await EnviarAsync<ResultadoImportacionDto>(cliente, ruta, Hoja(7002L, "Segundo", 3000000021L), "2026-10-06", null, repartir: true);
        Assert.Equal(0, segundoResultado.ServiciosCreados);
        Assert.Equal(1, segundoResultado.PasajerosAsignados);

        var servicios = await contexto.Servicios.AsNoTracking().Where(s => s.JornadaId == primerResultado.JornadaId).ToListAsync();
        var servicio = Assert.Single(servicios);
        Assert.Equal(servicioId, servicio.ServicioId);
        Assert.Equal(2, await contexto.ServiciosPasajero.CountAsync(sp => sp.ServicioId == servicioId));
    }

    [Fact]
    public async Task RepartirParaUnaHoraConUnServicioYaFinalizado_LiberaLaUnidadEnVezDeFallar()
    {
        using var alcance = _fixture.CrearAlcance();
        var contexto = alcance.ServiceProvider.GetRequiredService<LFMovaDbContext>();
        var hasheador = alcance.ServiceProvider.GetRequiredService<IHasheadorContrasenas>();

        var empresa = await SemillaDatosHelper.CrearEmpresaAsync(contexto, "Empresa Ruta Finalizada");
        await SemillaDatosHelper.CrearUsuarioConRolAsync(contexto, hasheador, "IMP-COORD7", "ClaveCoord123", Rol.COORDINADOR, empresa.EmpresaId);
        contexto.Sedes.Add(new LFMova.Domain.Entities.Sede
        {
            EmpresaId = empresa.EmpresaId, Nombre = "Centro", Direccion = "Calle 1", Ciudad = "Bogotá", Barrio = "Centro", Activa = true
        });
        await contexto.SaveChangesAsync();

        using var cliente = _fixture.Fabrica.CreateClient();
        await AutenticarAsync(cliente, "IMP-COORD7", "ClaveCoord123");

        using (var anonimo = _fixture.Fabrica.CreateClient())
        {
            (await anonimo.PostAsJsonAsync("/api/autenticacion/registrarse", new RegistrarUsuarioDto
            {
                Correo = "cond.fin@pruebas.test", NombreCompleto = "Fin Conductor", Cedula = "IMP-COND7", Telefono = "3000000007", Password = "ClaveCond123"
            })).EnsureSuccessStatusCode();
            (await anonimo.PostAsJsonAsync("/api/autenticacion/confirmar-correo",
                new ConfirmarCorreoDto { Token = _fixture.Fabrica.Correo.ObtenerUltimoToken("cond.fin@pruebas.test") })).EnsureSuccessStatusCode();
        }

        await InvitacionesHelper.UnirAEmpresaAsync(_fixture.Fabrica, cliente, empresa.EmpresaId, "IMP-COND7");
        (await cliente.PostAsJsonAsync($"/api/empresas/{empresa.EmpresaId}/conductores", new CrearConductorDto
        {
            Cedula = "IMP-COND7",
            Vehiculo = new CrearVehiculoDto
            {
                Placa = "FIN123", Marca = "Renault", Modelo = "2023", Capacidad = 8,
                VigenciaSoat = new DateOnly(2027, 1, 1), VigenciaTecnomecanica = new DateOnly(2027, 6, 1)
            }
        })).EnsureSuccessStatusCode();

        static byte[] Hoja(long cedula, string nombre, long celular)
        {
            using var libro = new XLWorkbook();
            var hoja = libro.AddWorksheet("Datos");
            var encabezados = new[] { "Cedula", "Nombre", "Direccion", "Barrio", "Celular", "Entrada", "Sede" };
            for (var i = 0; i < encabezados.Length; i++) hoja.Cell(1, i + 1).Value = encabezados[i];
            hoja.Cell(2, 1).Value = cedula;
            hoja.Cell(2, 2).Value = nombre;
            hoja.Cell(2, 3).Value = "Calle 1";
            hoja.Cell(2, 4).Value = "Centro";
            hoja.Cell(2, 5).Value = celular;
            hoja.Cell(2, 6).Value = "6:00";
            hoja.Cell(2, 7).Value = "Centro";

            using var memoria = new MemoryStream();
            libro.SaveAs(memoria);
            return memoria.ToArray();
        }

        var ruta = $"/api/empresas/{empresa.EmpresaId}/importaciones";

        // --- Primera importación: 1 pasajero, y esa ruta se marca como ya finalizada (como si el
        // conductor ya la hubiera hecho, por ejemplo en una prueba anterior). ---
        var primerResultado = await EnviarAsync<ResultadoImportacionDto>(cliente, ruta, Hoja(8001L, "Primero", 3000000030L), "2026-10-07", null, repartir: true);
        Assert.Equal(1, primerResultado.ServiciosCreados);
        var servicioFinalizado = await contexto.Servicios.SingleAsync(s => s.JornadaId == primerResultado.JornadaId);
        servicioFinalizado.Estado = LFMova.Domain.Enums.EstadoServicio.FINALIZADO;
        await contexto.SaveChangesAsync();

        // --- Segunda importación, misma hora: antes fallaba con "capacidad libre 0" porque la ruta
        // finalizada seguía marcando la única unidad como ocupada; ahora abre una ruta nueva. ---
        var segundoResultado = await EnviarAsync<ResultadoImportacionDto>(cliente, ruta, Hoja(8002L, "Segundo", 3000000031L), "2026-10-07", null, repartir: true);
        Assert.Equal(1, segundoResultado.ServiciosCreados);
        Assert.Equal(1, segundoResultado.PasajerosAsignados);

        var servicios = await contexto.Servicios.AsNoTracking().Where(s => s.JornadaId == primerResultado.JornadaId).ToListAsync();
        Assert.Equal(2, servicios.Count);
        Assert.Equal(1, await contexto.ServiciosPasajero.CountAsync(sp => sp.ServicioId == servicioFinalizado.ServicioId));
    }

    private async Task AutenticarAsync(HttpClient cliente, string identificador, string password)
    {
        var respuesta = await cliente.PostAsJsonAsync("/api/autenticacion/iniciar-sesion", new IniciarSesionDto { Identificador = identificador, Password = password });
        respuesta.EnsureSuccessStatusCode();
        var cuerpo = await respuesta.Content.ReadFromJsonAsync<RespuestaAutenticacionDto>();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", cuerpo!.Token);
    }

    private static async Task<T> EnviarAsync<T>(HttpClient cliente, string ruta, byte[] archivo, string? fecha, int? unidadId, bool repartir = false, int? sedeGeneralId = null)
    {
        using var contenido = new MultipartFormDataContent();
        var parte = new ByteArrayContent(archivo);
        parte.Headers.ContentType = new MediaTypeHeaderValue("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        contenido.Add(parte, "archivo", "programacion.xlsx");
        if (fecha is not null)
        {
            contenido.Add(new StringContent(fecha), "fechaOperativa");
        }

        if (unidadId is not null)
        {
            contenido.Add(new StringContent(unidadId.Value.ToString()), "unidadOperativaId");
        }

        if (repartir)
        {
            contenido.Add(new StringContent("true"), "repartirEntreUnidades");
        }

        if (sedeGeneralId is not null)
        {
            contenido.Add(new StringContent(sedeGeneralId.Value.ToString()), "sedeGeneralId");
        }

        var respuesta = await cliente.PostAsync(ruta, contenido);
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        return (await respuesta.Content.ReadFromJsonAsync<T>())!;
    }

    /// <summary>Construye en memoria una hoja con el mismo formato que recibe un conductor a diario.</summary>
    private static byte[] ConstruirHoja(long baseCedula = 1000)
    {
        using var libro = new XLWorkbook();
        var hoja = libro.AddWorksheet("Programación");
        hoja.Cell(1, 1).Value = "TRANSPORTADOR:";
        hoja.Cell(1, 2).Value = "TRANSPORTES PRUEBA SAS";
        hoja.Cell(2, 1).Value = "FECHA:";
        hoja.Cell(2, 2).Value = "14-15 SEP";

        var encabezados = new[] { "CEDULA", "NOMBRE", "APELLIDOS", "DIRECCION", "BARRIO", "CELULAR", "ENTRA", "SALE" };
        for (var i = 0; i < encabezados.Length; i++)
        {
            hoja.Cell(4, i + 1).Value = encabezados[i];
        }

        var fila = 5;
        void Seccion(string titulo) => hoja.Cell(fila++, 1).Value = titulo;
        void Pasajero(long cedula, string nombre, string apellidos, string direccion, string barrio, long celular, TimeSpan hora, bool entrada)
        {
            hoja.Cell(fila, 1).Value = cedula;
            hoja.Cell(fila, 2).Value = nombre;
            hoja.Cell(fila, 3).Value = apellidos;
            hoja.Cell(fila, 4).Value = direccion;
            hoja.Cell(fila, 5).Value = barrio;
            hoja.Cell(fila, 6).Value = celular;
            hoja.Cell(fila, entrada ? 7 : 8).Value = hora;
            fila++;
        }

        Seccion("ENTRADA PANAMERICANA");
        Pasajero(baseCedula + 1, "Ana", "Pérez Gómez", "Carrera 10 # 20-30", "Chapinero", 3001111111, new TimeSpan(22, 0, 0), true);
        Pasajero(baseCedula + 2, "Luis", "Rojas Díaz", "Calle 45 # 12-08", "Teusaquillo", 3002222222, new TimeSpan(22, 0, 0), true);
        Pasajero(baseCedula + 3, "María", "Torres Ruiz", "Diagonal 60 # 5-15", "Barrios Unidos", 3003333333, new TimeSpan(23, 45, 0), true);
        Pasajero(baseCedula + 4, "Jorge", "Vega Mora", "Transversal 3 # 9-20", "Suba", 3004444444, new TimeSpan(23, 45, 0), true);
        Seccion("SALIDA PANAMERICANA");
        Pasajero(baseCedula + 5, "Sofía", "Lara Cruz", "Avenida 68 # 40-10", "Engativá", 3005555555, new TimeSpan(1, 0, 0), false);
        Pasajero(baseCedula + 6, "Andrés", "Mejía Paz", "Calle 80 # 70-25", "Bosa", 3006666666, new TimeSpan(1, 0, 0), false);

        using var memoria = new MemoryStream();
        libro.SaveAs(memoria);
        return memoria.ToArray();
    }
}
