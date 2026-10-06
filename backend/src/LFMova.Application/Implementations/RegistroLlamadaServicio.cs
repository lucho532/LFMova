using LFMova.Application.DTOs.Llamadas;
using LFMova.Application.Interfaces;
using LFMova.Domain.Entities;
using LFMova.Domain.Rules;

namespace LFMova.Application.Implementations;

/// <summary>
/// Implementa <see cref="IRegistroLlamadaServicio"/>: guarda cada vez que el
/// conductor pulsa "Llamar" sobre un pasajero y la duración aproximada que
/// mide su dispositivo. No decide autorización de acceso al endpoint: eso se
/// verifica en la capa de Api.
/// </summary>
public class RegistroLlamadaServicio : IRegistroLlamadaServicio
{
    private readonly IRegistroLlamadaRepositorio _registroLlamadaRepositorio;
    private readonly IServicioPasajeroRepositorio _servicioPasajeroRepositorio;
    private readonly IServicioRepositorio _servicioRepositorio;

    /// <summary>Crea el servicio con sus repositorios.</summary>
    public RegistroLlamadaServicio(
        IRegistroLlamadaRepositorio registroLlamadaRepositorio,
        IServicioPasajeroRepositorio servicioPasajeroRepositorio,
        IServicioRepositorio servicioRepositorio)
    {
        _registroLlamadaRepositorio = registroLlamadaRepositorio;
        _servicioPasajeroRepositorio = servicioPasajeroRepositorio;
        _servicioRepositorio = servicioRepositorio;
    }

    /// <inheritdoc />
    public async Task<RegistroLlamadaDto> RegistrarAsync(int empresaId, int servicioPasajeroId, RegistrarLlamadaDto datos)
    {
        await ValidarPasajeroDeLaEmpresaAsync(empresaId, servicioPasajeroId);

        var registro = new RegistroLlamada
        {
            ServicioPasajeroId = servicioPasajeroId,
            FechaHora = DateTime.UtcNow,
            Latitud = datos.Latitud,
            Longitud = datos.Longitud
        };

        await _registroLlamadaRepositorio.AgregarAsync(registro);
        await _registroLlamadaRepositorio.GuardarCambiosAsync();
        return ADto(registro);
    }

    /// <inheritdoc />
    public async Task<RegistroLlamadaDto> RegistrarDuracionAsync(int empresaId, int servicioPasajeroId, int registroLlamadaId, int segundos)
    {
        await ValidarPasajeroDeLaEmpresaAsync(empresaId, servicioPasajeroId);

        var registro = await _registroLlamadaRepositorio.ObtenerPorIdAsync(registroLlamadaId);
        if (registro is null || registro.ServicioPasajeroId != servicioPasajeroId)
        {
            throw new InvalidOperationException("La llamada indicada no existe para este pasajero.");
        }

        // La duración se guarda una sola vez: un registro ya medido no se corrige después.
        if (registro.DuracionAproximadaSegundos is null && ReglasRegistroLlamada.EsDuracionVerosimil(segundos))
        {
            registro.DuracionAproximadaSegundos = segundos;
            await _registroLlamadaRepositorio.GuardarCambiosAsync();
        }

        return ADto(registro);
    }

    /// <inheritdoc />
    public async Task<List<RegistroLlamadaDto>> ObtenerPorServicioPasajeroAsync(int empresaId, int servicioPasajeroId)
    {
        await ValidarPasajeroDeLaEmpresaAsync(empresaId, servicioPasajeroId);

        var registros = await _registroLlamadaRepositorio.ObtenerPorServicioPasajeroAsync(servicioPasajeroId);
        return registros.Select(ADto).ToList();
    }

    private async Task ValidarPasajeroDeLaEmpresaAsync(int empresaId, int servicioPasajeroId)
    {
        var servicioPasajero = await _servicioPasajeroRepositorio.ObtenerPorIdAsync(servicioPasajeroId);
        var servicio = servicioPasajero is null ? null : await _servicioRepositorio.ObtenerPorIdAsync(servicioPasajero.ServicioId);
        if (servicio?.Jornada is null || !ReglasMultiempresa.JornadaPerteneceAEmpresa(servicio.Jornada, empresaId))
        {
            throw new InvalidOperationException("El pasajero indicado no existe en esta empresa.");
        }
    }

    private static RegistroLlamadaDto ADto(RegistroLlamada registro)
    {
        return new RegistroLlamadaDto
        {
            RegistroLlamadaId = registro.RegistroLlamadaId,
            ServicioPasajeroId = registro.ServicioPasajeroId,
            FechaHora = registro.FechaHora,
            DuracionAproximadaSegundos = registro.DuracionAproximadaSegundos,
            Latitud = registro.Latitud,
            Longitud = registro.Longitud
        };
    }
}
