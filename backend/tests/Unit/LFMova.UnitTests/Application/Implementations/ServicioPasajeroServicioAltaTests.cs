using LFMova.Application.DTOs.ServiciosPasajero;
using LFMova.Domain.Enums;

namespace LFMova.UnitTests.Application.Implementations;

/// <summary>
/// Pruebas de las rutas a las que ya no se pueden sumar pasajeros (decisión
/// del 2026-10-02). Usan el mismo armado que el resto de pruebas de alta.
/// </summary>
public partial class ServicioPasajeroServicioTests
{
    [Theory]
    [InlineData(EstadoServicio.EN_CURSO)]
    [InlineData(EstadoServicio.FINALIZADO)]
    [InlineData(EstadoServicio.CANCELADO)]
    public async Task CrearAsync_LanzaExcepcion_CuandoLaRutaYaEstaEnCursoFinalizadaOCancelada(EstadoServicio estado)
    {
        var jornada = CrearJornada();
        var servicioEntidad = CrearServicio(jornada);
        servicioEntidad.Estado = estado;
        var (servicio, _, _, _, _) = CrearServicioConEntidades(ProgramacionValida(), servicioEntidad);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            servicio.CrearAsync(EmpresaId, servicioEntidad.ServicioId, new CrearServicioPasajeroDto { ProgramacionTransporteId = 1 }));
    }
}
