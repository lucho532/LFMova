using LFMova.Application.DTOs.Importaciones;
using LFMova.Application.DTOs.Jornadas;
using LFMova.Application.DTOs.Programaciones;
using LFMova.Application.DTOs.Servicios;
using LFMova.Application.DTOs.ServiciosPasajero;
using LFMova.Application.Interfaces;
using LFMova.Application.Utils;
using LFMova.Application.Validators;
using LFMova.Domain.Enums;
using LFMova.Domain.Rules;

namespace LFMova.Application.Implementations.Importaciones;

/// <summary>
/// Crea una ruta nueva con los pasajeros que el coordinador pegó desde un Excel externo: abre el
/// servicio, asegura a cada empleado y su programación y lo asigna como pasajero. No interpreta el
/// pegado (eso ya lo resolvió el frontend) ni decide autorización.
/// </summary>
public class CreadorRutaPegada
{
    private readonly IJornadaServicio _jornadaServicio;
    private readonly IServicioServicio _servicioServicio;
    private readonly IProgramacionTransporteServicio _programacionServicio;
    private readonly AgregadorPasajeroServicio _agregador;

    /// <summary>Crea el colaborador con sus dependencias.</summary>
    public CreadorRutaPegada(
        IJornadaServicio jornadaServicio,
        IServicioServicio servicioServicio,
        IProgramacionTransporteServicio programacionServicio,
        AgregadorPasajeroServicio agregador)
    {
        _jornadaServicio = jornadaServicio;
        _servicioServicio = servicioServicio;
        _programacionServicio = programacionServicio;
        _agregador = agregador;
    }

    /// <summary>
    /// Crea la ruta y le agrega los pasajeros pegados. Las filas inválidas o repetidas se omiten con un
    /// aviso; si ninguna pudo agregarse, la ruta recién creada se elimina y se informa como fallo.
    /// </summary>
    public async Task<ResultadoImportacionDto> CrearAsync(int empresaId, CrearRutaPegadaDto datos)
    {
        if (datos.Pasajeros.Count == 0)
        {
            throw new InvalidOperationException("No hay ningún pasajero para crear la ruta.");
        }

        // A diferencia de CreadorRutaVacia, acá siempre se crea una ruta nueva (nunca se reutiliza una
        // existente): cada pegado del coordinador representa explícitamente una tarjeta nueva. Si no se
        // indica unidad, queda sin conductor (PENDIENTE_ASIGNACION) para asignárselo después desde ahí
        // mismo, igual que una ruta importada que no alcanzó a repartirse por falta de unidades; si sí
        // se indica, queda asignada de una vez (CrearAsync ya valida que la unidad esté disponible para
        // ese horario).
        var jornadas = await _jornadaServicio.ObtenerPorEmpresaAsync(empresaId);
        var jornada = jornadas.FirstOrDefault(j => j.FechaOperativa == datos.Fecha)
            ?? await _jornadaServicio.CrearAsync(empresaId, new CrearJornadaDto { FechaOperativa = datos.Fecha });

        var servicio = await _servicioServicio.CrearAsync(empresaId, jornada.JornadaId, new CrearServicioDto
        {
            SedeId = datos.SedeId,
            Fecha = datos.Fecha,
            HoraProgramada = datos.Hora,
            Tipo = datos.Tipo,
            UnidadOperativaId = datos.UnidadOperativaId
        });
        await _servicioServicio.CambiarEstadoAsync(empresaId, servicio.ServicioId, new CambiarEstadoServicioDto { NuevoEstado = EstadoServicio.PENDIENTE_ASIGNACION });
        if (datos.UnidadOperativaId is not null)
        {
            await _servicioServicio.CambiarEstadoAsync(empresaId, servicio.ServicioId, new CambiarEstadoServicioDto { NuevoEstado = EstadoServicio.ASIGNADO });
        }
        servicio = await _servicioServicio.ObtenerPorIdAsync(empresaId, servicio.ServicioId);

        var resultado = new ResultadoImportacionDto { JornadaId = jornada.JornadaId, ServicioId = servicio!.ServicioId, ServiciosCreados = 1 };

        // Filas inválidas o con una cédula repetida dentro del mismo pegado no abortan toda la creación:
        // se omiten con un aviso (mismo criterio que la importación por Excel) y el resto sí se agrega.
        var programaciones = await _programacionServicio.ObtenerPorEmpresaAsync(empresaId);
        var cedulasYaProcesadas = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var indice = 0; indice < datos.Pasajeros.Count; indice++)
        {
            var pasajero = datos.Pasajeros[indice];
            try
            {
                ValidarDatosDePasajero(pasajero.Cedula, pasajero.NombreCompleto, pasajero.Direccion, pasajero.Barrio);
                if (!cedulasYaProcesadas.Add(pasajero.Cedula.Trim()))
                {
                    resultado.FilasOmitidas++;
                    resultado.Advertencias.Add($"Fila {indice + 1}: la cédula {pasajero.Cedula.Trim()} está repetida en el pegado, se omitió.");
                    continue;
                }

                var fila = new FilaHoja
                {
                    Cedula = pasajero.Cedula.Trim(),
                    NombreCompleto = pasajero.NombreCompleto.Trim(),
                    Celular = pasajero.Celular.Trim(),
                    Direccion = pasajero.Direccion.Trim(),
                    Barrio = pasajero.Barrio.Trim()
                };
                // Si se indicó unidad, todos estos pasajeros son parte de la ruta recién creada (no avisos
                // de "pasajero de última hora" uno por uno): si el conductor necesita enterarse de su ruta
                // nueva, eso se resuelve aparte, no con N notificaciones repetidas.
                await _agregador.AgregarAsync(empresaId, servicio, fila, programaciones, resultado, notificarConductorSiTieneUnidad: datos.UnidadOperativaId is null);
            }
            catch (InvalidOperationException excepcion)
            {
                resultado.FilasOmitidas++;
                resultado.Advertencias.Add($"Fila {indice + 1}: {excepcion.Message}");
            }
        }

        // Si ninguna fila pudo agregarse (todas fallaron o venían repetidas), no queda una ruta vacía
        // esperando pasajeros: se elimina la que se acababa de crear y se informa como fallo, no como éxito.
        if (resultado.PasajerosAsignados == 0)
        {
            await _servicioServicio.EliminarAsync(empresaId, servicio.ServicioId);
            throw new InvalidOperationException(
                resultado.Advertencias.Count > 0 ? string.Join(" ", resultado.Advertencias) : "No se pudo agregar a ningún pasajero.");
        }

        return resultado;
    }

    private static void ValidarDatosDePasajero(string cedula, string nombreCompleto, string direccion, string barrio)
    {
        if (!CedulaValidador.EsValida(cedula))
        {
            throw new InvalidOperationException("La cédula es obligatoria.");
        }

        if (string.IsNullOrWhiteSpace(nombreCompleto) || string.IsNullOrWhiteSpace(direccion) || string.IsNullOrWhiteSpace(barrio))
        {
            throw new InvalidOperationException("El nombre, la dirección y el barrio son obligatorios.");
        }
    }
}
