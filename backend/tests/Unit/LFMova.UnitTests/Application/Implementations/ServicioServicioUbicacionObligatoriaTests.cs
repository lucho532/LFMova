using LFMova.Application.DTOs.Servicios;
using LFMova.Domain.Enums;

namespace LFMova.UnitTests.Application.Implementations;

/// <summary>
/// Pruebas de la regla "inicio y fin de ruta siempre llevan la ubicación del conductor":
/// sin coordenadas válidas el servicio no cambia de estado ni se guarda nada.
/// </summary>
public partial class ServicioServicioTests
{
    public static IEnumerable<object?[]> UbicacionesInvalidas() =>
    [
        [null, null],
        [4.65, null],
        [null, -74.05],
        [91.0, -74.05],
        [4.65, -181.0],
    ];

    [Theory]
    [MemberData(nameof(UbicacionesInvalidas))]
    public async Task IniciarAsync_LanzaArgumentException_CuandoFaltaLaUbicacion(double? latitud, double? longitud)
    {
        var (jornadas, sedes, servicios, unidades, conductores) = CrearRepositorios();
        var servicio = CrearServicio(servicios, jornadas, sedes, unidades, conductores);
        var creado = await servicio.CrearAsync(1, 1, DtoValido(UnidadActivaId));
        await servicio.CambiarEstadoAsync(1, creado.ServicioId, new CambiarEstadoServicioDto { NuevoEstado = EstadoServicio.PENDIENTE_ASIGNACION });
        await servicio.CambiarEstadoAsync(1, creado.ServicioId, new CambiarEstadoServicioDto { NuevoEstado = EstadoServicio.ASIGNADO });
        await servicio.CambiarEstadoAsync(1, creado.ServicioId, new CambiarEstadoServicioDto { NuevoEstado = EstadoServicio.PUBLICADO });

        await Assert.ThrowsAsync<ArgumentException>(() =>
            servicio.IniciarAsync(1, creado.ServicioId, new IniciarServicioDto { Latitud = latitud, Longitud = longitud }));

        var sinCambios = await servicio.ObtenerPorIdAsync(1, creado.ServicioId);
        Assert.Equal(EstadoServicio.PUBLICADO, sinCambios!.Estado);
        Assert.Null(sinCambios.HoraInicioReal);
    }

    [Fact]
    public async Task IniciarAsync_LanzaArgumentException_CuandoNoSeEnviaCuerpo()
    {
        var (jornadas, sedes, servicios, unidades, conductores) = CrearRepositorios();
        var servicio = CrearServicio(servicios, jornadas, sedes, unidades, conductores);
        var creado = await servicio.CrearAsync(1, 1, DtoValido(UnidadActivaId));
        await servicio.CambiarEstadoAsync(1, creado.ServicioId, new CambiarEstadoServicioDto { NuevoEstado = EstadoServicio.PENDIENTE_ASIGNACION });
        await servicio.CambiarEstadoAsync(1, creado.ServicioId, new CambiarEstadoServicioDto { NuevoEstado = EstadoServicio.ASIGNADO });
        await servicio.CambiarEstadoAsync(1, creado.ServicioId, new CambiarEstadoServicioDto { NuevoEstado = EstadoServicio.PUBLICADO });

        await Assert.ThrowsAsync<ArgumentException>(() => servicio.IniciarAsync(1, creado.ServicioId));
    }

    [Theory]
    [MemberData(nameof(UbicacionesInvalidas))]
    public async Task FinalizarAsync_LanzaArgumentException_CuandoFaltaLaUbicacion(double? latitud, double? longitud)
    {
        var (servicio, creado) = await CrearServicioEnCursoAsync();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            servicio.FinalizarAsync(1, creado.ServicioId, new FinalizarServicioDto { Latitud = latitud, Longitud = longitud }));

        var sinCambios = await servicio.ObtenerPorIdAsync(1, creado.ServicioId);
        Assert.Equal(EstadoServicio.EN_CURSO, sinCambios!.Estado);
        Assert.Null(sinCambios.HoraFinReal);
    }
}
