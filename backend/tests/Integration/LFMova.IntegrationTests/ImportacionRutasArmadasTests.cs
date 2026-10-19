using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using LFMova.Application.DTOs.Importaciones;
using LFMova.Application.DTOs.Servicios;
using LFMova.Application.DTOs.UnidadesOperativas;
using LFMova.Domain.Enums;
using LFMova.IntegrationTests.Fixtures;

namespace LFMova.IntegrationTests;

/// <summary>
/// Importación de un Excel con rutas ya armadas contra una base real: las rutas se crean tal como vienen
/// (una por bloque de filas con el mismo número de ruta, pasajeros en el orden de las filas, sin repartir ni reordenar), la conductora se
/// toma de la placa, lo que no se puede asignar queda sin conductor y nada se publica.
/// </summary>
public partial class ImportacionExcelTests
{
    private static byte[] HojaConRutasArmadas()
    {
        using var libro = new XLWorkbook();
        var hoja = libro.AddWorksheet("Hoja1");
        var encabezados = new[] { "Cedula", "Nombre", "Direccion", "Barrio", "Celular", "Hora Entrada", "Fecha", "No RUTA", "PLACA", "Sede" };
        for (var i = 0; i < encabezados.Length; i++)
        {
            hoja.Cell(1, i + 1).Value = encabezados[i];
        }

        // La ruta 1 tiene mas pasajeros que la capacidad (3) y se repite en un segundo bloque (separado por una linea de conductor):
        // son dos rutas del mismo vehiculo a la misma hora. La ruta 2 trae un numero interno en vez de placa.
        var filas = new (long Cedula, string Nombre, int Ruta, string Placa)[]
        {
            (8301, "Ana Uno", 1, "rparm"), (8302, "Beto Dos", 1, "RPARM"), (8303, "Cleo Tres", 1, "RPARM"), (8304, "Dani Cuatro", 1, "RPARM"),
            (0, "OTRO BLOQUE", 0, string.Empty),
            (8306, "Fabi Seis", 1, "RPARM"), (8307, "Gus Siete", 1, "RPARM"),
            (8305, "Eva Cinco", 2, "103")
        };
        for (var i = 0; i < filas.Length; i++)
        {
            var fila = i + 2;
            if (filas[i].Cedula == 0)
            {
                hoja.Cell(fila, 2).Value = filas[i].Nombre;
                continue;
            }

            hoja.Cell(fila, 1).Value = filas[i].Cedula;
            hoja.Cell(fila, 2).Value = filas[i].Nombre;
            hoja.Cell(fila, 3).Value = $"Calle {i} # 1-1";
            hoja.Cell(fila, 4).Value = "Centro";
            hoja.Cell(fila, 5).Value = 3100000000L + i;
            hoja.Cell(fila, 6).Value = new TimeSpan(5, 0, 0);
            hoja.Cell(fila, 7).Value = new DateTime(2026, 12, 27);
            hoja.Cell(fila, 8).Value = filas[i].Ruta;
            hoja.Cell(fila, 9).Value = filas[i].Placa;
            hoja.Cell(fila, 10).Value = "Centro";
        }

        using var memoria = new MemoryStream();
        libro.SaveAs(memoria);
        return memoria.ToArray();
    }

    private static async Task<HttpResponseMessage> EnviarRutasArmadasAsync(HttpClient cliente, string url, byte[] archivo, string? fecha = null)
    {
        using var contenido = new MultipartFormDataContent();
        var parte = new ByteArrayContent(archivo);
        parte.Headers.ContentType = new MediaTypeHeaderValue("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        contenido.Add(parte, "archivo", "rutas.xlsx");
        contenido.Add(new StringContent("COLUMNA_NUMERO_RUTA"), "modo");
        if (fecha is not null)
        {
            contenido.Add(new StringContent(fecha), "fechaOperativa");
        }

        return await cliente.PostAsync(url, contenido);
    }

    [Fact]
    public async Task RutasArmadas_SeCreanTalComoVienenConLaUnidadDeLaPlacaYSinPublicar()
    {
        var (cliente, ruta, contexto) = await PrepararEmpresaConUnConductorAsync("ARM");
        using var _ = cliente;
        var url = $"{ruta}/rutas-armadas";
        var archivo = HojaConRutasArmadas();

        // --- Validar: dos rutas, la de la placa conocida con conductor y la otra sin él; nada se guarda. ---
        var respuestaVista = await EnviarRutasArmadasAsync(cliente, $"{url}/validar", archivo);
        Assert.Equal(HttpStatusCode.OK, respuestaVista.StatusCode);
        var vista = (await respuestaVista.Content.ReadFromJsonAsync<VistaPreviaRutasArmadasDto>())!;
        Assert.True(vista.PuedeImportar, string.Join(" | ", vista.Errores));
        Assert.Equal(new[] { "1", "1 (bloque 2)", "2" }, vista.Rutas.Select(r => r.Referencia));
        Assert.Equal(new[] { 4, 2, 1 }, vista.Rutas.Select(r => r.Pasajeros.Count));
        Assert.NotNull(vista.Rutas[0].Conductor);
        Assert.Equal(vista.Rutas[0].Conductor, vista.Rutas[1].Conductor);
        Assert.Null(vista.Rutas[2].Conductor);
        Assert.Contains(vista.Advertencias, a => a.Contains("capacidad 3"));
        Assert.Contains(vista.Advertencias, a => a.Contains("\"103\""));
        Assert.False(vista.FaltaSede);

        // --- Importar. ---
        var respuesta = await EnviarRutasArmadasAsync(cliente, url, archivo, "2026-12-27");
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var resultado = (await respuesta.Content.ReadFromJsonAsync<ResultadoImportacionDto>())!;
        Assert.Equal(3, resultado.ServiciosCreados);
        Assert.Equal(7, resultado.PasajerosAsignados);

        var servicios = await contexto.Servicios.AsNoTracking().Where(s => s.JornadaId == resultado.JornadaId).OrderBy(s => s.ServicioId).ToListAsync();
        Assert.Equal(3, servicios.Count);
        // Las dos rutas del mismo vehiculo a la misma hora quedan asignadas a la misma unidad (rutas dobles).
        Assert.Equal(EstadoServicio.ASIGNADO, servicios[0].Estado);
        Assert.NotNull(servicios[0].UnidadOperativaId);
        Assert.Equal(EstadoServicio.ASIGNADO, servicios[1].Estado);
        Assert.Equal(servicios[0].UnidadOperativaId, servicios[1].UnidadOperativaId);
        Assert.Equal(servicios[0].HoraProgramada, servicios[1].HoraProgramada);
        Assert.Equal(EstadoServicio.PENDIENTE_ASIGNACION, servicios[2].Estado);
        Assert.Null(servicios[2].UnidadOperativaId);

        // Los pasajeros de cada bloque quedan en el orden exacto de las filas y no se mezclan entre bloques.
        async Task<List<string>> CedulasAsync(int servicioId) => await contexto.ServiciosPasajero.AsNoTracking().Where(sp => sp.ServicioId == servicioId)
            .OrderBy(sp => sp.Orden).Select(sp => sp.Empleado!.Usuario!.Cedula).ToListAsync();
        Assert.Equal(new[] { "8301", "8302", "8303", "8304" }, await CedulasAsync(servicios[0].ServicioId));
        Assert.Equal(new[] { "8306", "8307" }, await CedulasAsync(servicios[1].ServicioId));

        // --- Reimportar el mismo archivo no duplica nada. ---
        var repetido = (await (await EnviarRutasArmadasAsync(cliente, url, archivo, "2026-12-27")).Content.ReadFromJsonAsync<ResultadoImportacionDto>())!;
        Assert.Equal(0, repetido.ServiciosCreados);
        Assert.Equal(0, repetido.PasajerosAsignados);
        Assert.Equal(7, repetido.FilasOmitidas);
        Assert.Equal(3, await contexto.Servicios.CountAsync(s => s.JornadaId == resultado.JornadaId));
    }

    [Fact]
    public async Task NumeroInterno_SeAsignaPorEmpresaYSirveParaReconocerLaUnidadEnLasRutasArmadas()
    {
        var (cliente, ruta, contexto) = await PrepararEmpresaConUnConductorAsync("NUM");
        using var _ = cliente;
        var (otroCliente, _, _) = await PrepararEmpresaConUnConductorAsync("NU2");
        using var __ = otroCliente;
        var empresaId = int.Parse(ruta.Split('/')[3]);
        var unidadId = await contexto.UnidadesOperativas.Where(u => u.Vehiculo!.Placa == "RPNUM").Select(u => u.UnidadOperativaId).SingleAsync();
        var urlNumero = $"/api/empresas/{empresaId}/unidades-operativas/{unidadId}/numero-interno";
        var archivo = HojaConRutasArmadas();

        // Sin número interno, la placa "103" del archivo no se reconoce y la ruta queda sin conductor.
        var antes = (await (await EnviarRutasArmadasAsync(cliente, $"{ruta}/rutas-armadas/validar", archivo)).Content.ReadFromJsonAsync<VistaPreviaRutasArmadasDto>())!;
        Assert.Null(antes.Rutas.Single(r => r.Placa == "103").Conductor);

        // Otra empresa no puede ponerle número a una unidad que no es suya.
        Assert.Equal(HttpStatusCode.Forbidden, (await otroCliente.PutAsJsonAsync(urlNumero, new AsignarNumeroInternoDto { Numero = "103" })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await cliente.PutAsJsonAsync(urlNumero, new AsignarNumeroInternoDto { Numero = new string('9', 31) })).StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await cliente.PutAsJsonAsync(urlNumero, new AsignarNumeroInternoDto { Numero = " 103 " })).StatusCode);
        var numeros = (await cliente.GetFromJsonAsync<List<NumeroInternoUnidadDto>>($"/api/empresas/{empresaId}/unidades-operativas/numeros-internos"))!;
        var numero = Assert.Single(numeros);
        Assert.Equal((unidadId, "103"), (numero.UnidadOperativaId, numero.Numero));

        // Con el número registrado, la ruta que trae "103" ya toma el conductor de esa unidad.
        var despues = (await (await EnviarRutasArmadasAsync(cliente, $"{ruta}/rutas-armadas/validar", archivo)).Content.ReadFromJsonAsync<VistaPreviaRutasArmadasDto>())!;
        var conNumero = despues.Rutas.Single(r => r.Placa == "103");
        Assert.NotNull(conNumero.Conductor);
        Assert.Equal("103", conNumero.NumeroInterno);

        // Quitarlo (vacío) deja la unidad sin número.
        Assert.Equal(HttpStatusCode.NoContent, (await cliente.PutAsJsonAsync(urlNumero, new AsignarNumeroInternoDto { Numero = "" })).StatusCode);
        Assert.Empty((await cliente.GetFromJsonAsync<List<NumeroInternoUnidadDto>>($"/api/empresas/{empresaId}/unidades-operativas/numeros-internos"))!);
    }

    private static byte[] HojaDeUnPasajeroConCelular(string celular, string direccion = "Calle 1 # 1-1", string barrio = "Centro", string nombre = "Pedro Telefono", long cedula = 8401L, string placa = "RPTEL", string fecha = "2026-12-28")
    {
        using var libro = new XLWorkbook();
        var hoja = libro.AddWorksheet("Hoja1");
        var encabezados = new[] { "Cedula", "Nombre", "Direccion", "Barrio", "Celular", "Hora Entrada", "Fecha", "No RUTA", "PLACA", "Sede" };
        for (var i = 0; i < encabezados.Length; i++)
        {
            hoja.Cell(1, i + 1).Value = encabezados[i];
        }

        hoja.Cell(2, 1).Value = cedula;
        hoja.Cell(2, 2).Value = nombre;
        hoja.Cell(2, 3).Value = direccion;
        hoja.Cell(2, 4).Value = barrio;
        hoja.Cell(2, 5).Value = celular;
        hoja.Cell(2, 6).Value = new TimeSpan(5, 0, 0);
        hoja.Cell(2, 7).Value = DateTime.Parse(fecha);
        hoja.Cell(2, 8).Value = 1;
        hoja.Cell(2, 9).Value = placa;
        hoja.Cell(2, 10).Value = "Centro";
        using var memoria = new MemoryStream();
        libro.SaveAs(memoria);
        return memoria.ToArray();
    }

    [Fact]
    public async Task ReimportarActualizaElTelefonoPeroNuncaCambiaUnCelularPorUnFijo()
    {
        var (cliente, ruta, contexto) = await PrepararEmpresaConUnConductorAsync("TEL");
        using var _ = cliente;
        var url = $"{ruta}/rutas-armadas";
        Task<T> Leer<T>(HttpResponseMessage r) => r.Content.ReadFromJsonAsync<T>()!;
        async Task<string> TelefonoAsync() => await contexto.Empleados.AsNoTracking().Where(e => e.Usuario!.Cedula == "8401").Select(e => e.Telefono).SingleAsync();
        async Task ImportarAsync(string celular) => (await EnviarRutasArmadasAsync(cliente, url, HojaDeUnPasajeroConCelular(celular), "2026-12-28")).EnsureSuccessStatusCode();

        // Primera importación con el celular en cero (como en el archivo del cliente): queda sin teléfono útil.
        await ImportarAsync("0");
        Assert.Equal(string.Empty, await TelefonoAsync());

        // Un fijo (como el que se tomaba por error de la columna TELEFONO) llena el vacío...
        await ImportarAsync("8915645");
        Assert.Equal("8915645", await TelefonoAsync());

        // ...pero el celular lo mejora, aunque el pasajero ya estuviera en la ruta.
        var tercera = await Leer<ResultadoImportacionDto>(await EnviarRutasArmadasAsync(cliente, url, HojaDeUnPasajeroConCelular("3227001122"), "2026-12-28"));
        Assert.Equal(1, tercera.FilasOmitidas);
        Assert.Equal("3227001122", await TelefonoAsync());

        // Si el empleado cambia de celular, el archivo lo actualiza y queda un aviso para revisarlo.
        var cuarta = await Leer<ResultadoImportacionDto>(await EnviarRutasArmadasAsync(cliente, url, HojaDeUnPasajeroConCelular("3009998877"), "2026-12-28"));
        Assert.Equal("3009998877", await TelefonoAsync());
        Assert.Contains(cuarta.Advertencias, a => a.Contains("3227001122") && a.Contains("3009998877"));

        // Un archivo que solo trae un fijo no le quita el celular.
        await ImportarAsync("8928691");
        Assert.Equal("3009998877", await TelefonoAsync());
    }

    [Fact]
    public async Task ReimportarActualizaNombreDireccionYBarrioPeroNoLaCedulaNiElHistorico()
    {
        var (cliente, ruta, contexto) = await PrepararEmpresaConUnConductorAsync("DAT");
        using var _ = cliente;
        var url = $"{ruta}/rutas-armadas";

        (await EnviarRutasArmadasAsync(cliente, url, HojaDeUnPasajeroConCelular("3117924418", cedula: 8402L), "2026-12-29")).EnsureSuccessStatusCode();
        var resultado = await (await EnviarRutasArmadasAsync(
            cliente, url, HojaDeUnPasajeroConCelular("3117924418", direccion: "Carrera 9 # 8-7", barrio: "Prado", nombre: "Pedro Nuevo Nombre", cedula: 8402L), "2026-12-29")).Content.ReadFromJsonAsync<ResultadoImportacionDto>();

        var empleado = await contexto.Empleados.AsNoTracking().Include(e => e.Usuario).SingleAsync(e => e.Usuario!.Cedula == "8402");
        Assert.Equal(("Pedro Nuevo Nombre", "Carrera 9 # 8-7", "Prado"), (empleado.NombreCompleto, empleado.Direccion, empleado.Barrio));
        Assert.Equal("8402", empleado.Usuario!.Cedula);
        Assert.Contains(resultado!.Advertencias, a => a.Contains("nombre") && a.Contains("dirección") && a.Contains("barrio"));

        // El pasajero de la ruta ya creada conserva la dirección con la que se creó (el histórico no cambia).
        var pasajero = await contexto.ServiciosPasajero.AsNoTracking().SingleAsync(sp => sp.EmpleadoId == empleado.EmpleadoId);
        Assert.Equal("Calle 1 # 1-1", pasajero.DireccionRecogida);
    }

    [Fact]
    public async Task CambiarSedeDeUnaRuta_MueveLaRutaYLaProgramacionDeSusPasajerosYRespetaLasReglas()
    {
        var (cliente, ruta, contexto) = await PrepararEmpresaConUnConductorAsync("SED");
        using var _ = cliente;
        var empresaId = int.Parse(ruta.Split('/')[3]);
        var norte = new LFMova.Domain.Entities.Sede { EmpresaId = empresaId, Nombre = "Norte", Direccion = "Calle 2", Ciudad = "Manizales", Barrio = "Norte", Activa = true };
        var ajena = new LFMova.Domain.Entities.Sede { EmpresaId = (await SemillaDatosHelper.CrearEmpresaAsync(contexto, "Empresa ajena SED")).EmpresaId, Nombre = "Ajena", Direccion = "x", Ciudad = "x", Barrio = "x", Activa = true };
        contexto.Sedes.AddRange(norte, ajena);
        await contexto.SaveChangesAsync();
        var centroId = await contexto.Sedes.Where(s => s.EmpresaId == empresaId && s.Nombre == "Centro").Select(s => s.SedeId).SingleAsync();

        var resultado = (await (await EnviarRutasArmadasAsync(cliente, $"{ruta}/rutas-armadas", HojaDeUnPasajeroConCelular("3117924418", cedula: 8403L, placa: "RPSED", fecha: "2026-12-30"), "2026-12-30")).Content.ReadFromJsonAsync<ResultadoImportacionDto>())!;
        var servicioId = resultado.ServicioId ?? (await contexto.Servicios.AsNoTracking().Where(s => s.JornadaId == resultado.JornadaId).Select(s => s.ServicioId).SingleAsync());
        var url = $"{ruta.Replace("importaciones", "jornadas")}/{resultado.JornadaId}/servicios/{servicioId}/cambiar-sede";
        async Task<HttpResponseMessage> CambiarAsync(int sedeId) => await cliente.PostAsJsonAsync(url, new CambiarSedeServicioDto { SedeId = sedeId });

        // Cambia la sede de la ruta y la de la programación de su pasajero.
        Assert.Equal(HttpStatusCode.NoContent, (await CambiarAsync(norte.SedeId)).StatusCode);
        Assert.Equal(norte.SedeId, (await contexto.Servicios.AsNoTracking().SingleAsync(s => s.ServicioId == servicioId)).SedeId);
        var sedesDeProgramaciones = await contexto.ServiciosPasajero.AsNoTracking().Where(sp => sp.ServicioId == servicioId).Select(sp => sp.ProgramacionTransporte!.SedeId).ToListAsync();
        Assert.Equal(new[] { norte.SedeId }, sedesDeProgramaciones);

        // Una sede de otra empresa o inexistente se rechaza.
        Assert.Equal(HttpStatusCode.Conflict, (await CambiarAsync(ajena.SedeId)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await CambiarAsync(999999)).StatusCode);

        // Una ruta publicada vuelve a Asignado al cambiarle la sede.
        await contexto.Servicios.Where(s => s.ServicioId == servicioId).ExecuteUpdateAsync(c => c.SetProperty(s => s.Estado, EstadoServicio.PUBLICADO));
        Assert.Equal(HttpStatusCode.NoContent, (await CambiarAsync(centroId)).StatusCode);
        var despues = await contexto.Servicios.AsNoTracking().SingleAsync(s => s.ServicioId == servicioId);
        Assert.Equal((centroId, EstadoServicio.ASIGNADO), (despues.SedeId, despues.Estado));

        // Una ruta en curso ya no se puede cambiar.
        await contexto.Servicios.Where(s => s.ServicioId == servicioId).ExecuteUpdateAsync(c => c.SetProperty(s => s.Estado, EstadoServicio.EN_CURSO));
        Assert.Equal(HttpStatusCode.Conflict, (await CambiarAsync(norte.SedeId)).StatusCode);
    }
}
