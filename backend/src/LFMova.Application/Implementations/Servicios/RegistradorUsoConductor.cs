using LFMova.Application.Interfaces;
using LFMova.Domain.Entities;
using LFMova.Domain.Enums;
using LFMova.Domain.Rules;

namespace LFMova.Application.Implementations.Servicios;

/// <summary>
/// Anota, al finalizar una ruta, quién la condujo: el registro con el que se
/// factura el mes (ver <see cref="UsoConductor"/>). Copia los datos del
/// conductor y de su vehículo para que el registro no dependa de que sigan
/// existiendo. No guarda por su cuenta: el registro se persiste junto con la
/// finalización de la ruta.
/// </summary>
public class RegistradorUsoConductor
{
    private readonly IFacturacionRepositorio _facturacionRepositorio;
    private readonly IUnidadOperativaRepositorio _unidadOperativaRepositorio;
    private readonly IConductorRepositorio _conductorRepositorio;
    private readonly IVehiculoRepositorio _vehiculoRepositorio;

    /// <summary>Crea el colaborador con sus repositorios.</summary>
    public RegistradorUsoConductor(
        IFacturacionRepositorio facturacionRepositorio,
        IUnidadOperativaRepositorio unidadOperativaRepositorio,
        IConductorRepositorio conductorRepositorio,
        IVehiculoRepositorio vehiculoRepositorio)
    {
        _facturacionRepositorio = facturacionRepositorio;
        _unidadOperativaRepositorio = unidadOperativaRepositorio;
        _conductorRepositorio = conductorRepositorio;
        _vehiculoRepositorio = vehiculoRepositorio;
    }

    /// <summary>
    /// Prepara el registro de uso de la ruta recién finalizada. No hace nada
    /// si la ruta no tiene unidad o su conductor ya no existe (no hay a quién
    /// atribuirla).
    /// </summary>
    public async Task RegistrarAsync(Servicio ruta, IEnumerable<ServicioPasajero> pasajeros, DateTime finalizadaUtc)
    {
        if (ruta.UnidadOperativaId is null || ruta.Jornada is null)
        {
            return;
        }

        var unidad = await _unidadOperativaRepositorio.ObtenerPorIdAsync(ruta.UnidadOperativaId.Value);
        var conductor = unidad is null ? null : await _conductorRepositorio.ObtenerPorIdAsync(unidad.ConductorId);
        if (unidad is null || conductor is null)
        {
            return;
        }

        var vehiculo = await _vehiculoRepositorio.ObtenerPorIdAsync(unidad.VehiculoId);
        await _facturacionRepositorio.AgregarUsoAsync(new UsoConductor
        {
            EmpresaId = ruta.Jornada.EmpresaId,
            ServicioId = ruta.ServicioId,
            Fecha = ReglasFacturacion.DiaColombia(finalizadaUtc),
            ConductorId = conductor.ConductorId,
            Cedula = conductor.Usuario?.Cedula ?? string.Empty,
            NombreConductor = conductor.NombreCompleto,
            Placa = vehiculo?.Placa ?? string.Empty,
            PasajerosTransportados = pasajeros.Count(p => p.Estado == EstadoServicioPasajero.RECOGIDO)
        });
    }
}
