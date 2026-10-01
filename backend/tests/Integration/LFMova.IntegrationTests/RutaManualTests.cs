using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using LFMova.Application.DTOs.Autenticacion;
using LFMova.Application.DTOs.Importaciones;
using LFMova.Application.DTOs.Sedes;
using LFMova.Application.DTOs.Servicios;
using LFMova.Application.DTOs.ServiciosPasajero;
using LFMova.Application.Interfaces;
using LFMova.Domain.Entities;
using LFMova.Domain.Enums;
using LFMova.Infrastructure.Data;
using LFMova.IntegrationTests.Fixtures;

namespace LFMova.IntegrationTests;

/// <summary>
/// Prueba el alta manual de rutas del coordinador (sin Excel): crear una
/// ruta a mano reutilizando o creando la jornada y el servicio según haga
/// falta, y las acciones sobre cada pasajero de una ruta ya creada
/// (reasignar de unidad, cancelar, eliminar). También verifica que "rutas
/// programadas" en las estadísticas solo cuenta las ya publicadas.
/// </summary>
[Collection(IntegrationTestCollection.Nombre)]
public class RutaManualTests
{
    private readonly IntegrationTestFixture _fixture;

    /// <summary>Crea la prueba con la colección compartida de PostgreSQL.</summary>
    public RutaManualTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task CrearRutaVacia_ReutilizaSiYaExiste_YSirveComoDestinoParaMoverUnPasajero()
    {
        using var alcance = _fixture.CrearAlcance();
        var contexto = alcance.ServiceProvider.GetRequiredService<LFMovaDbContext>();
        var hasheador = alcance.ServiceProvider.GetRequiredService<IHasheadorContrasenas>();

        var empresa = await SemillaDatosHelper.CrearEmpresaAsync(contexto, "Empresa Ruta Vacía");
        await SemillaDatosHelper.CrearUsuarioConRolAsync(contexto, hasheador, "RV-COORD", "ClaveCoord123", Rol.COORDINADOR, empresa.EmpresaId);

        var unidadId = new Dictionary<string, int>();
        foreach (var (cedula, placa) in new[] { ("RV-C1", "VAC001"), ("RV-C2", "VAC002") })
        {
            var usuario = await SemillaDatosHelper.CrearUsuarioConRolAsync(contexto, hasheador, cedula, "ClaveCond123", Rol.CONDUCTOR, empresa.EmpresaId);
            var conductor = new Conductor { UsuarioId = usuario.UsuarioId, NombreCompleto = cedula, Telefono = "3000000000", Activo = true };
            contexto.Conductores.Add(conductor);
            await contexto.SaveChangesAsync();
            contexto.VinculacionesConductorEmpresa.Add(new VinculacionConductorEmpresa { ConductorId = conductor.ConductorId, EmpresaId = empresa.EmpresaId, Activa = true });
            var vehiculo = new Vehiculo { ConductorId = conductor.ConductorId, Placa = placa, Marca = "X", Modelo = "2023", Capacidad = 4, Activo = true };
            contexto.Vehiculos.Add(vehiculo);
            await contexto.SaveChangesAsync();
            var unidad = new UnidadOperativa { ConductorId = conductor.ConductorId, VehiculoId = vehiculo.VehiculoId, Activa = true };
            contexto.UnidadesOperativas.Add(unidad);
            await contexto.SaveChangesAsync();
            unidadId[cedula] = unidad.UnidadOperativaId;
        }

        using var cliente = _fixture.Fabrica.CreateClient();
        await AutenticarAsync(cliente, "RV-COORD", "ClaveCoord123");

        var respuestaSede = await cliente.PostAsJsonAsync($"/api/empresas/{empresa.EmpresaId}/sedes", new CrearSedeDto
        {
            Nombre = "Sede Vacía", Direccion = "Calle 1", Ciudad = "Bogotá", Barrio = "Centro"
        });
        respuestaSede.EnsureSuccessStatusCode();
        var sede = (await respuestaSede.Content.ReadFromJsonAsync<SedeDto>())!;

        // La ruta original: dos pasajeros con la unidad 1, todos los barrios de la ciudad mezclados
        // (el caso real que motivó esto: una ruta "sobrante" que absorbió todo lo que quedó sin conductor).
        var respuestaOriginal = await cliente.PostAsJsonAsync($"/api/empresas/{empresa.EmpresaId}/importaciones/ruta-pegada", new CrearRutaPegadaDto
        {
            Fecha = new DateOnly(2026, 10, 1), Hora = new TimeOnly(6, 0), Tipo = TipoServicio.ENTRADA, SedeId = sede.SedeId, UnidadOperativaId = unidadId["RV-C1"],
            Pasajeros = new List<FilaPasajeroPegadoDto>
            {
                new() { Cedula = "RV-1001", NombreCompleto = "Ana Original", Celular = "3000000001", Direccion = "Calle 1 # 2-3", Barrio = "Centro" },
                new() { Cedula = "RV-1002", NombreCompleto = "Luis Original", Celular = "3000000001", Direccion = "Calle 1 # 2-3", Barrio = "Centro" },
            }
        });
        Assert.Equal(HttpStatusCode.OK, respuestaOriginal.StatusCode);
        var original = (await respuestaOriginal.Content.ReadFromJsonAsync<ResultadoImportacionDto>())!;

        var rutaVacia = $"/api/empresas/{empresa.EmpresaId}/importaciones/ruta-vacia";
        var datosRutaVacia = new CrearRutaVaciaDto
        {
            Fecha = new DateOnly(2026, 10, 1), Hora = new TimeOnly(6, 0), Tipo = TipoServicio.ENTRADA, SedeId = sede.SedeId, UnidadOperativaId = unidadId["RV-C2"]
        };

        // Se abre la ruta nueva, vacía, para dividir la original.
        var respuestaVacia = await cliente.PostAsJsonAsync(rutaVacia, datosRutaVacia);
        Assert.Equal(HttpStatusCode.OK, respuestaVacia.StatusCode);
        var nueva = (await respuestaVacia.Content.ReadFromJsonAsync<ResultadoImportacionDto>())!;
        Assert.Equal(1, nueva.ServiciosCreados);
        Assert.NotEqual(original.ServicioId, nueva.ServicioId);
        Assert.Equal(EstadoServicio.ASIGNADO, (await contexto.Servicios.AsNoTracking().FirstAsync(s => s.ServicioId == nueva.ServicioId)).Estado);
        Assert.Empty(await contexto.ServiciosPasajero.AsNoTracking().Where(p => p.ServicioId == nueva.ServicioId).ToListAsync());

        // Pedirla otra vez con los mismos datos la reutiliza en vez de crear una tercera ruta.
        var respuestaReutilizada = await cliente.PostAsJsonAsync(rutaVacia, datosRutaVacia);
        var reutilizada = (await respuestaReutilizada.Content.ReadFromJsonAsync<ResultadoImportacionDto>())!;
        Assert.Equal(0, reutilizada.ServiciosCreados);
        Assert.Equal(nueva.ServicioId, reutilizada.ServicioId);

        // Se mueve uno de los pasajeros de la ruta original a la ruta nueva: la división real.
        var rutaPasajerosOriginal = $"/api/empresas/{empresa.EmpresaId}/jornadas/{original.JornadaId}/servicios/{original.ServicioId}/pasajeros";
        var pasajeros = await cliente.GetFromJsonAsync<List<ServicioPasajeroDto>>(rutaPasajerosOriginal);
        var idLuis = pasajeros!.Single(p => p.NombreCompletoEmpleado == "Luis Original").ServicioPasajeroId;

        var mover = await cliente.PutAsJsonAsync($"{rutaPasajerosOriginal}/{idLuis}/mover", new MoverServicioPasajeroDto { ServicioDestinoId = nueva.ServicioId!.Value });
        Assert.Equal(HttpStatusCode.NoContent, mover.StatusCode);

        Assert.Single(await contexto.ServiciosPasajero.AsNoTracking().Where(p => p.ServicioId == original.ServicioId).ToListAsync());
        Assert.Single(await contexto.ServiciosPasajero.AsNoTracking().Where(p => p.ServicioId == nueva.ServicioId).ToListAsync());

        // Una ruta cancelada o finalizada no puede reutilizarse como destino para abrir de nuevo.
        var rutaVaciaOtroHorario = new CrearRutaVaciaDto
        {
            Fecha = new DateOnly(2026, 10, 1), Hora = new TimeOnly(7, 0), Tipo = TipoServicio.ENTRADA, SedeId = sede.SedeId, UnidadOperativaId = unidadId["RV-C2"]
        };
        var otraRuta = await cliente.PostAsJsonAsync(rutaVacia, rutaVaciaOtroHorario);
        var otra = (await otraRuta.Content.ReadFromJsonAsync<ResultadoImportacionDto>())!;
        var servicioCancelado = await contexto.Servicios.FirstAsync(s => s.ServicioId == otra.ServicioId);
        servicioCancelado.Estado = EstadoServicio.CANCELADO;
        await contexto.SaveChangesAsync();

        var reutilizarCancelada = await cliente.PostAsJsonAsync(rutaVacia, rutaVaciaOtroHorario);
        Assert.Equal(HttpStatusCode.Conflict, reutilizarCancelada.StatusCode);
    }

    [Fact]
    public async Task ServiciosPendientes_IncluyeRutasSinEnviarDeFechasPasadas_YExcluyeLasFinalizadasOCanceladas()
    {
        using var alcance = _fixture.CrearAlcance();
        var contexto = alcance.ServiceProvider.GetRequiredService<LFMovaDbContext>();
        var hasheador = alcance.ServiceProvider.GetRequiredService<IHasheadorContrasenas>();

        var empresa = await SemillaDatosHelper.CrearEmpresaAsync(contexto, "Empresa Pendientes");
        await SemillaDatosHelper.CrearUsuarioConRolAsync(contexto, hasheador, "PEND-COORD", "ClaveCoord123", Rol.COORDINADOR, empresa.EmpresaId);

        using var cliente = _fixture.Fabrica.CreateClient();
        await AutenticarAsync(cliente, "PEND-COORD", "ClaveCoord123");

        var sede = (await (await cliente.PostAsJsonAsync($"/api/empresas/{empresa.EmpresaId}/sedes", new CrearSedeDto
        {
            Nombre = "Sede Pendientes", Direccion = "Calle 1", Ciudad = "Bogotá", Barrio = "Centro"
        })).Content.ReadFromJsonAsync<SedeDto>())!;

        async Task<int> CrearRutaAsync(DateOnly fecha, string cedula)
        {
            var respuesta = await cliente.PostAsJsonAsync($"/api/empresas/{empresa.EmpresaId}/importaciones/ruta-pegada", new CrearRutaPegadaDto
            {
                Fecha = fecha, Hora = new TimeOnly(6, 0), Tipo = TipoServicio.ENTRADA, SedeId = sede.SedeId,
                Pasajeros = new List<FilaPasajeroPegadoDto>
                {
                    new() { Cedula = cedula, NombreCompleto = "Pasajero Pendiente", Celular = "3000000001", Direccion = "Calle 1 # 2-3", Barrio = "Centro" }
                }
            });
            respuesta.EnsureSuccessStatusCode();
            return (await respuesta.Content.ReadFromJsonAsync<ResultadoImportacionDto>())!.ServicioId!.Value;
        }

        // Una ruta sin enviar de una fecha ya pasada (por ejemplo, un Excel con la fecha de otro día) debe seguir visible.
        var pasadaSinEnviar = await CrearRutaAsync(new DateOnly(2026, 9, 15), "PEND-1");
        var futura = await CrearRutaAsync(new DateOnly(2026, 10, 20), "PEND-2");
        var cancelada = await CrearRutaAsync(new DateOnly(2026, 10, 21), "PEND-3");
        var servicioCancelado = await contexto.Servicios.SingleAsync(s => s.ServicioId == cancelada);
        servicioCancelado.Estado = EstadoServicio.CANCELADO;
        await contexto.SaveChangesAsync();

        var pendientes = await cliente.GetFromJsonAsync<List<ServicioDto>>(
            $"/api/empresas/{empresa.EmpresaId}/jornadas/servicios-pendientes?desde=2026-09-27");

        var ids = pendientes!.Select(s => s.ServicioId).ToList();
        Assert.Contains(pasadaSinEnviar, ids);
        Assert.Contains(futura, ids);
        Assert.DoesNotContain(cancelada, ids);
        // Vienen de distintas jornadas (una por fecha) y ordenadas por fecha.
        Assert.Equal(new[] { pasadaSinEnviar, futura }, ids);
    }

    [Fact]
    public async Task CrearRutaPegada_CreaLaRutaSinConductor_YOmiteFilasInvalidasORepetidas()
    {
        using var alcance = _fixture.CrearAlcance();
        var contexto = alcance.ServiceProvider.GetRequiredService<LFMovaDbContext>();
        var hasheador = alcance.ServiceProvider.GetRequiredService<IHasheadorContrasenas>();

        var empresa = await SemillaDatosHelper.CrearEmpresaAsync(contexto, "Empresa Ruta Pegada");
        await SemillaDatosHelper.CrearUsuarioConRolAsync(contexto, hasheador, "RP-COORD", "ClaveCoord123", Rol.COORDINADOR, empresa.EmpresaId);

        using var cliente = _fixture.Fabrica.CreateClient();
        await AutenticarAsync(cliente, "RP-COORD", "ClaveCoord123");

        var respuestaSede = await cliente.PostAsJsonAsync($"/api/empresas/{empresa.EmpresaId}/sedes", new CrearSedeDto
        {
            Nombre = "Sede Pegada", Direccion = "Calle 1", Ciudad = "Bogotá", Barrio = "Centro"
        });
        respuestaSede.EnsureSuccessStatusCode();
        var sede = (await respuestaSede.Content.ReadFromJsonAsync<SedeDto>())!;

        var ruta = $"/api/empresas/{empresa.EmpresaId}/importaciones/ruta-pegada";
        var respuesta = await cliente.PostAsJsonAsync(ruta, new CrearRutaPegadaDto
        {
            Fecha = new DateOnly(2026, 10, 1),
            Hora = new TimeOnly(6, 0),
            Tipo = TipoServicio.ENTRADA,
            SedeId = sede.SedeId,
            Pasajeros = new List<FilaPasajeroPegadoDto>
            {
                new() { Cedula = "RP-1001", NombreCompleto = "Ana Pegada", Celular = "3000000001", Direccion = "Calle 1 # 2-3", Barrio = "Centro" },
                new() { Cedula = "RP-1002", NombreCompleto = "Luis Pegado", Celular = "3000000002", Direccion = "Calle 4 # 5-6", Barrio = "Centro" },
                // Repetida: la segunda vez debe omitirse, no duplicarse.
                new() { Cedula = "RP-1001", NombreCompleto = "Ana Pegada", Celular = "3000000001", Direccion = "Calle 1 # 2-3", Barrio = "Centro" },
                // Inválida: sin barrio.
                new() { Cedula = "RP-1003", NombreCompleto = "Fila Inválida", Celular = "3000000003", Direccion = "Calle 7 # 8-9", Barrio = "" },
            }
        });

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var resultado = (await respuesta.Content.ReadFromJsonAsync<ResultadoImportacionDto>())!;
        Assert.Equal(1, resultado.ServiciosCreados);
        Assert.Equal(2, resultado.PasajerosAsignados);
        Assert.Equal(2, resultado.FilasOmitidas);
        Assert.Equal(2, resultado.Advertencias.Count);

        var servicio = await contexto.Servicios.AsNoTracking().SingleAsync(s => s.ServicioId == resultado.ServicioId);
        Assert.Equal(EstadoServicio.PENDIENTE_ASIGNACION, servicio.Estado);
        Assert.Null(servicio.UnidadOperativaId);
        Assert.Equal(2, await contexto.ServiciosPasajero.AsNoTracking().CountAsync(p => p.ServicioId == resultado.ServicioId));

        // Un pegado nuevo (aunque sea con los mismos datos de fecha/hora/tipo/sede) crea OTRA ruta: nunca reutiliza.
        var segunda = await cliente.PostAsJsonAsync(ruta, new CrearRutaPegadaDto
        {
            Fecha = new DateOnly(2026, 10, 1),
            Hora = new TimeOnly(6, 0),
            Tipo = TipoServicio.ENTRADA,
            SedeId = sede.SedeId,
            Pasajeros = new List<FilaPasajeroPegadoDto> { new() { Cedula = "RP-2001", NombreCompleto = "Otra Persona", Celular = "3000000009", Direccion = "Calle 9", Barrio = "Centro" } }
        });
        var resultadoSegunda = (await segunda.Content.ReadFromJsonAsync<ResultadoImportacionDto>())!;
        Assert.NotEqual(resultado.ServicioId, resultadoSegunda.ServicioId);
        Assert.Equal(resultado.JornadaId, resultadoSegunda.JornadaId);
    }

    [Fact]
    public async Task CrearRutaPegada_AsignaElConductorDeUnaVez_CuandoSeIndicaUnidadOperativa()
    {
        using var alcance = _fixture.CrearAlcance();
        var contexto = alcance.ServiceProvider.GetRequiredService<LFMovaDbContext>();
        var hasheador = alcance.ServiceProvider.GetRequiredService<IHasheadorContrasenas>();

        var empresa = await SemillaDatosHelper.CrearEmpresaAsync(contexto, "Empresa Ruta Pegada Con Conductor");
        await SemillaDatosHelper.CrearUsuarioConRolAsync(contexto, hasheador, "RPC-COORD", "ClaveCoord123", Rol.COORDINADOR, empresa.EmpresaId);

        var usuarioConductor = await SemillaDatosHelper.CrearUsuarioConRolAsync(contexto, hasheador, "RPC-COND", "ClaveCond123", Rol.CONDUCTOR, empresa.EmpresaId);
        var conductor = new Conductor { UsuarioId = usuarioConductor.UsuarioId, NombreCompleto = "RPC-COND", Telefono = "3000000000", Activo = true };
        contexto.Conductores.Add(conductor);
        await contexto.SaveChangesAsync();
        contexto.VinculacionesConductorEmpresa.Add(new VinculacionConductorEmpresa { ConductorId = conductor.ConductorId, EmpresaId = empresa.EmpresaId, Activa = true });
        var vehiculo = new Vehiculo { ConductorId = conductor.ConductorId, Placa = "RPC001", Marca = "X", Modelo = "2023", Capacidad = 4, Activo = true };
        contexto.Vehiculos.Add(vehiculo);
        await contexto.SaveChangesAsync();
        var unidad = new UnidadOperativa { ConductorId = conductor.ConductorId, VehiculoId = vehiculo.VehiculoId, Activa = true };
        contexto.UnidadesOperativas.Add(unidad);
        await contexto.SaveChangesAsync();

        using var cliente = _fixture.Fabrica.CreateClient();
        await AutenticarAsync(cliente, "RPC-COORD", "ClaveCoord123");

        var respuestaSede = await cliente.PostAsJsonAsync($"/api/empresas/{empresa.EmpresaId}/sedes", new CrearSedeDto
        {
            Nombre = "Sede Pegada Con Conductor", Direccion = "Calle 1", Ciudad = "Bogotá", Barrio = "Centro"
        });
        respuestaSede.EnsureSuccessStatusCode();
        var sede = (await respuestaSede.Content.ReadFromJsonAsync<SedeDto>())!;

        var ruta = $"/api/empresas/{empresa.EmpresaId}/importaciones/ruta-pegada";
        var respuesta = await cliente.PostAsJsonAsync(ruta, new CrearRutaPegadaDto
        {
            Fecha = new DateOnly(2026, 10, 3),
            Hora = new TimeOnly(6, 0),
            Tipo = TipoServicio.ENTRADA,
            SedeId = sede.SedeId,
            UnidadOperativaId = unidad.UnidadOperativaId,
            Pasajeros = new List<FilaPasajeroPegadoDto>
            {
                new() { Cedula = "RPC-1001", NombreCompleto = "Ana Con Conductor", Celular = "3000000001", Direccion = "Calle 1 # 2-3", Barrio = "Centro" },
                new() { Cedula = "RPC-1002", NombreCompleto = "Luis Con Conductor", Celular = "3000000002", Direccion = "Calle 4 # 5-6", Barrio = "Centro" },
            }
        });

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var resultado = (await respuesta.Content.ReadFromJsonAsync<ResultadoImportacionDto>())!;
        Assert.Equal(2, resultado.PasajerosAsignados);

        var servicio = await contexto.Servicios.AsNoTracking().SingleAsync(s => s.ServicioId == resultado.ServicioId);
        Assert.Equal(EstadoServicio.ASIGNADO, servicio.Estado);
        Assert.Equal(unidad.UnidadOperativaId, servicio.UnidadOperativaId);

        // No se manda una notificación de "pasajero de última hora" por cada uno: es la ruta recién creada, no una modificación posterior.
        var notificacionesConductor = await contexto.Notificaciones.AsNoTracking().Where(n => n.UsuarioId == usuarioConductor.UsuarioId).ToListAsync();
        Assert.DoesNotContain(notificacionesConductor, n => n.Tipo == "RUTA_MODIFICADA");
    }

    [Fact]
    public async Task CrearRutaPegada_EliminaLaRutaYRespondeConflicto_CuandoNingunaFilaSePudoAgregar()
    {
        using var alcance = _fixture.CrearAlcance();
        var contexto = alcance.ServiceProvider.GetRequiredService<LFMovaDbContext>();
        var hasheador = alcance.ServiceProvider.GetRequiredService<IHasheadorContrasenas>();

        var empresa = await SemillaDatosHelper.CrearEmpresaAsync(contexto, "Empresa Ruta Pegada Vacía");
        await SemillaDatosHelper.CrearUsuarioConRolAsync(contexto, hasheador, "RPV-COORD", "ClaveCoord123", Rol.COORDINADOR, empresa.EmpresaId);

        using var cliente = _fixture.Fabrica.CreateClient();
        await AutenticarAsync(cliente, "RPV-COORD", "ClaveCoord123");

        var respuestaSede = await cliente.PostAsJsonAsync($"/api/empresas/{empresa.EmpresaId}/sedes", new CrearSedeDto
        {
            Nombre = "Sede Pegada Vacía", Direccion = "Calle 1", Ciudad = "Bogotá", Barrio = "Centro"
        });
        respuestaSede.EnsureSuccessStatusCode();
        var sede = (await respuestaSede.Content.ReadFromJsonAsync<SedeDto>())!;

        var fecha = new DateOnly(2026, 10, 2);
        var ruta = $"/api/empresas/{empresa.EmpresaId}/importaciones/ruta-pegada";

        // La única fila viene sin barrio (inválida): no se agrega ningún pasajero, así que la ruta que se
        // acababa de crear para recibirlos no debe quedar huérfana (ver AGENTS.md §24: vacía es permitido
        // solo temporalmente durante la reorganización, no como resultado normal de esta operación).
        var respuesta = await cliente.PostAsJsonAsync(ruta, new CrearRutaPegadaDto
        {
            Fecha = fecha,
            Hora = new TimeOnly(6, 0),
            Tipo = TipoServicio.ENTRADA,
            SedeId = sede.SedeId,
            Pasajeros = new List<FilaPasajeroPegadoDto>
            {
                new() { Cedula = "RPV-1001", NombreCompleto = "Fila Inválida", Celular = "3000000001", Direccion = "Calle 1 # 2-3", Barrio = "" },
            }
        });

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
        Assert.Empty(await contexto.Servicios.AsNoTracking().Where(s => s.Fecha == fecha).ToListAsync());
    }

    [Fact]
    public async Task PlantillaColumnasPegado_NoExisteAlPrincipio_YGuardarLaReemplazaEnVezDeDuplicarla()
    {
        using var alcance = _fixture.CrearAlcance();
        var contexto = alcance.ServiceProvider.GetRequiredService<LFMovaDbContext>();
        var hasheador = alcance.ServiceProvider.GetRequiredService<IHasheadorContrasenas>();

        var empresa = await SemillaDatosHelper.CrearEmpresaAsync(contexto, "Empresa Plantilla Pegado");
        await SemillaDatosHelper.CrearUsuarioConRolAsync(contexto, hasheador, "PP-COORD", "ClaveCoord123", Rol.COORDINADOR, empresa.EmpresaId);

        using var cliente = _fixture.Fabrica.CreateClient();
        await AutenticarAsync(cliente, "PP-COORD", "ClaveCoord123");

        var ruta = $"/api/empresas/{empresa.EmpresaId}/importaciones/plantilla-columnas-pegado";

        var antes = await cliente.GetAsync(ruta);
        Assert.Equal(HttpStatusCode.NotFound, antes.StatusCode);

        var guardar = await cliente.PutAsJsonAsync(ruta, new GuardarPlantillaColumnasPegadoDto
        {
            ColumnasEnOrden = new List<string> { "CEDULA", "NOMBRE", "BARRIO", "DIRECCION", "CELULAR" }
        });
        Assert.Equal(HttpStatusCode.OK, guardar.StatusCode);

        var despues = await cliente.GetFromJsonAsync<PlantillaColumnasPegadoDto>(ruta);
        Assert.Equal(new[] { "CEDULA", "NOMBRE", "BARRIO", "DIRECCION", "CELULAR" }, despues!.ColumnasEnOrden);

        var reemplazar = await cliente.PutAsJsonAsync(ruta, new GuardarPlantillaColumnasPegadoDto
        {
            ColumnasEnOrden = new List<string> { "NOMBRE", "CEDULA", "CELULAR", "DIRECCION", "BARRIO" }
        });
        var actualizada = (await reemplazar.Content.ReadFromJsonAsync<PlantillaColumnasPegadoDto>())!;
        Assert.Equal(despues.PlantillaColumnasPegadoId, actualizada.PlantillaColumnasPegadoId);
        Assert.Equal(1, await contexto.PlantillasColumnasPegado.CountAsync(p => p.EmpresaId == empresa.EmpresaId));

        var conFormatoInvalido = await cliente.PutAsJsonAsync(ruta, new GuardarPlantillaColumnasPegadoDto { ColumnasEnOrden = new List<string> { "CEDULA", "NOMBRE" } });
        Assert.Equal(HttpStatusCode.Conflict, conFormatoInvalido.StatusCode);
    }

    private async Task AutenticarAsync(HttpClient cliente, string identificador, string password)
    {
        var respuesta = await cliente.PostAsJsonAsync("/api/autenticacion/iniciar-sesion", new IniciarSesionDto { Identificador = identificador, Password = password });
        respuesta.EnsureSuccessStatusCode();
        var cuerpo = await respuesta.Content.ReadFromJsonAsync<RespuestaAutenticacionDto>();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", cuerpo!.Token);
    }

}
