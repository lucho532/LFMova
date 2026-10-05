using LFMova.Application.DTOs.Servicios;
using LFMova.Domain.Entities;
using LFMova.Domain.Enums;

namespace LFMova.UnitTests.Application.Implementations;

/// <summary>
/// Pruebas del registro de facturación que se anota al finalizar una ruta
/// (quién la condujo y cuántos pasajeros recogió). Usan los mismos
/// repositorios falsos que el resto de pruebas de <c>ServicioServicio</c>.
/// </summary>
public partial class ServicioServicioTests
{
    [Fact]
    public async Task FinalizarAsync_AnotaLaRutaParaFacturarConLosDatosDelConductor()
    {
        var (jornadas, sedes, servicios, unidades, conductores) = CrearRepositorios();
        var conductor = await conductores.ObtenerPorIdAsync(ConductorVinculadoId);
        conductor!.NombreCompleto = "Carlos Conductor";
        conductor.Usuario = new Usuario { UsuarioId = 50, Cedula = "777" };
        var pasajeros = new ServicioPasajeroRepositorioFalso(
            new ServicioPasajero { ServicioPasajeroId = 1, ServicioId = 1, EmpleadoId = 1, Estado = EstadoServicioPasajero.RECOGIDO },
            new ServicioPasajero { ServicioPasajeroId = 2, ServicioId = 1, EmpleadoId = 2, Estado = EstadoServicioPasajero.NO_RECOGIDO });
        var facturacion = new FacturacionRepositorioEnMemoria();
        var servicio = CrearServicio(servicios, jornadas, sedes, unidades, conductores, pasajeros: pasajeros, facturacion: facturacion);
        var creado = await servicio.CrearAsync(1, 1, DtoValido(UnidadActivaId));
        await servicio.CambiarEstadoAsync(1, creado.ServicioId, new CambiarEstadoServicioDto { NuevoEstado = EstadoServicio.PENDIENTE_ASIGNACION });
        await servicio.CambiarEstadoAsync(1, creado.ServicioId, new CambiarEstadoServicioDto { NuevoEstado = EstadoServicio.ASIGNADO });
        await servicio.CambiarEstadoAsync(1, creado.ServicioId, new CambiarEstadoServicioDto { NuevoEstado = EstadoServicio.PUBLICADO });
        await servicio.IniciarAsync(1, creado.ServicioId);
        Assert.Empty(facturacion.Usos);

        await servicio.FinalizarAsync(1, creado.ServicioId);

        var uso = Assert.Single(facturacion.Usos);
        Assert.Equal(1, uso.EmpresaId);
        Assert.Equal(creado.ServicioId, uso.ServicioId);
        Assert.Equal("777", uso.Cedula);
        Assert.Equal("Carlos Conductor", uso.NombreConductor);
        Assert.Equal(ConductorVinculadoId, uso.ConductorId);
        // Solo cuenta al pasajero recogido, no al que quedó con incidencia.
        Assert.Equal(1, uso.PasajerosTransportados);
        // El día es el de la finalización en hora de Colombia (UTC-5).
        Assert.Equal(DateOnly.FromDateTime(DateTime.UtcNow.AddHours(-5)), uso.Fecha);
    }

    [Fact]
    public async Task FinalizarAsync_NoAnotaNada_CuandoLaFinalizacionSeRechaza()
    {
        var (jornadas, sedes, servicios, unidades, conductores) = CrearRepositorios();
        var pasajeros = new ServicioPasajeroRepositorioFalso(
            new ServicioPasajero { ServicioPasajeroId = 1, ServicioId = 1, EmpleadoId = 1, Estado = EstadoServicioPasajero.PROGRAMADO });
        var facturacion = new FacturacionRepositorioEnMemoria();
        var servicio = CrearServicio(servicios, jornadas, sedes, unidades, conductores, pasajeros: pasajeros, facturacion: facturacion);
        var creado = await servicio.CrearAsync(1, 1, DtoValido(UnidadActivaId));
        await servicio.CambiarEstadoAsync(1, creado.ServicioId, new CambiarEstadoServicioDto { NuevoEstado = EstadoServicio.PENDIENTE_ASIGNACION });
        await servicio.CambiarEstadoAsync(1, creado.ServicioId, new CambiarEstadoServicioDto { NuevoEstado = EstadoServicio.ASIGNADO });
        await servicio.CambiarEstadoAsync(1, creado.ServicioId, new CambiarEstadoServicioDto { NuevoEstado = EstadoServicio.PUBLICADO });
        await servicio.IniciarAsync(1, creado.ServicioId);

        await Assert.ThrowsAsync<InvalidOperationException>(() => servicio.FinalizarAsync(1, creado.ServicioId));

        Assert.Empty(facturacion.Usos);
    }
}
