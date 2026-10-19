using LFMova.Application.DTOs.Importaciones;
using LFMova.Application.DTOs.Jornadas;
using LFMova.Application.DTOs.Programaciones;
using LFMova.Application.DTOs.Servicios;
using LFMova.Application.Interfaces;
using LFMova.Application.Validators;
using LFMova.Domain.Enums;

namespace LFMova.Application.Implementations.Importaciones;

/// <summary>
/// Crea las rutas de un archivo con rutas armadas exactamente como vienen: un servicio por cada ruta, con
/// sus pasajeros en el orden del archivo y la unidad que corresponde a su placa. No reparte, no reordena,
/// no mueve pasajeros entre rutas ni publica: las rutas quedan listas para que el coordinador las revise.
/// No es una transacción única: si una ruta falla, las anteriores se conservan. No interpreta el archivo.
/// </summary>
public class EjecutorRutasArmadas
{
    private readonly IJornadaServicio _jornadaServicio;
    private readonly IServicioServicio _servicioServicio;
    private readonly IProgramacionTransporteServicio _programacionServicio;
    private readonly ResolutorSedeImportacion _resolutorSede;
    private readonly AgregadorPasajeroServicio _agregador;

    /// <summary>Crea el colaborador con sus dependencias.</summary>
    public EjecutorRutasArmadas(
        IJornadaServicio jornadaServicio,
        IServicioServicio servicioServicio,
        IProgramacionTransporteServicio programacionServicio,
        ResolutorSedeImportacion resolutorSede,
        AgregadorPasajeroServicio agregador)
    {
        _jornadaServicio = jornadaServicio;
        _servicioServicio = servicioServicio;
        _programacionServicio = programacionServicio;
        _resolutorSede = resolutorSede;
        _agregador = agregador;
    }

    /// <summary>
    /// Crea una ruta por cada ruta del archivo. <paramref name="fechaOperativa"/> se usa para las rutas
    /// que no traen fecha y <paramref name="sedeGeneralId"/> para las que no traen sede.
    /// </summary>
    public async Task EjecutarAsync(int empresaId, AnalisisRutasArmadas analisis, DateOnly fechaOperativa, int? sedeGeneralId, ResultadoImportacionDto resultado)
    {
        var sedes = await _resolutorSede.ObtenerActivasAsync(empresaId);
        if (analisis.Hoja.Rutas.Any(r => r.NombreSede.Trim().Length == 0) && sedes.All(s => s.SedeId != sedeGeneralId))
        {
            throw new InvalidOperationException("El archivo no trae la sede de algunas rutas: elige una sede al importar.");
        }

        var jornadas = (await _jornadaServicio.ObtenerPorEmpresaAsync(empresaId)).ToDictionary(j => j.FechaOperativa);
        var programaciones = await _programacionServicio.ObtenerPorEmpresaAsync(empresaId);

        for (var indice = 0; indice < analisis.Hoja.Rutas.Count; indice++)
        {
            var ruta = analisis.Hoja.Rutas[indice];
            var sede = await _resolutorSede.ObtenerOCrearAsync(empresaId, ruta.NombreSede, sedeGeneralId, sedes, resultado);
            var fecha = ruta.Fecha ?? fechaOperativa;
            if (!jornadas.TryGetValue(fecha, out var jornada))
            {
                jornada = await _jornadaServicio.CrearAsync(empresaId, new CrearJornadaDto { FechaOperativa = fecha });
                jornadas[fecha] = jornada;
            }

            await CrearRutaAsync(empresaId, jornada, ruta, sede.SedeId, fecha, analisis.UnidadPorRuta[indice]?.UnidadOperativaId, programaciones, resultado);
        }
    }

    private async Task CrearRutaAsync(
        int empresaId, JornadaDto jornada, RutaArmadaHoja ruta, int sedeId, DateOnly fecha, int? unidadOperativaId, List<ProgramacionDto> programaciones, ResultadoImportacionDto resultado)
    {
        var servicio = await CrearServicioAsync(empresaId, jornada, ruta, sedeId, fecha, unidadOperativaId, resultado);
        var agregados = 0;
        foreach (var fila in ruta.Filas)
        {
            try
            {
                if (!CedulaValidador.EsValida(fila.Cedula) || string.IsNullOrWhiteSpace(fila.NombreCompleto) || string.IsNullOrWhiteSpace(fila.Direccion) || string.IsNullOrWhiteSpace(fila.Barrio))
                {
                    throw new InvalidOperationException("faltan la cédula, el nombre, la dirección o el barrio.");
                }

                // Los pasajeros forman parte de la ruta recién creada: no se avisa de "última hora" uno por uno.
                await _agregador.AgregarAsync(empresaId, servicio, fila, programaciones, resultado, notificarConductorSiTieneUnidad: false);
                agregados++;
            }
            catch (InvalidOperationException excepcion)
            {
                resultado.FilasOmitidas++;
                resultado.Advertencias.Add($"Ruta \"{ruta.Referencia}\", fila {fila.NumeroFila}: se omitió, {char.ToLowerInvariant(excepcion.Message[0])}{excepcion.Message[1..]}");
            }
        }

        // No queda una ruta vacía esperando pasajeros: si ninguna fila pudo agregarse, se elimina y se avisa.
        if (agregados == 0)
        {
            await _servicioServicio.EliminarAsync(empresaId, servicio.ServicioId);
            resultado.ServiciosCreados--;
            resultado.Advertencias.Add($"La ruta \"{ruta.Referencia}\" no se creó: no se pudo agregar ningún pasajero.");
        }
    }

    private async Task<ServicioDto> CrearServicioAsync(
        int empresaId, JornadaDto jornada, RutaArmadaHoja ruta, int sedeId, DateOnly fecha, int? unidadOperativaId, ResultadoImportacionDto resultado)
    {
        var servicio = await _servicioServicio.CrearAsync(
            empresaId, jornada.JornadaId, new CrearServicioDto { SedeId = sedeId, Fecha = fecha, HoraProgramada = ruta.Hora, Tipo = ruta.Tipo });
        await _servicioServicio.CambiarEstadoAsync(empresaId, servicio.ServicioId, new CambiarEstadoServicioDto { NuevoEstado = EstadoServicio.PENDIENTE_ASIGNACION });
        resultado.JornadaId = jornada.JornadaId;
        resultado.ServiciosCreados++;

        if (unidadOperativaId is not null)
        {
            try
            {
                // El archivo puede traer rutas dobles (dos rutas del mismo vehículo a la misma hora, para que el conductor las cobre
                // por separado): se respeta, por eso aquí no se rechaza el cruce de horario de la unidad.
                await _servicioServicio.AsignarUnidadDeRutaArmadaAsync(empresaId, servicio.ServicioId, unidadOperativaId.Value);
            }
            catch (InvalidOperationException excepcion)
            {
                resultado.Advertencias.Add($"La ruta \"{ruta.Referencia}\" quedó sin conductor: {excepcion.Message}");
            }
        }

        return (await _servicioServicio.ObtenerPorIdAsync(empresaId, servicio.ServicioId))!;
    }
}
