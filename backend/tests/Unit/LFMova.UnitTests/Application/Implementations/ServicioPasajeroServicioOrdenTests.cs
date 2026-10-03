using LFMova.Application.DTOs.ServiciosPasajero;
using LFMova.Domain.Entities;
using LFMova.Domain.Enums;

namespace LFMova.UnitTests.Application.Implementations;

/// <summary>
/// Pruebas del cambio de orden de un pasajero dentro de su ruta: mueve al
/// pasajero a la posición pedida y renumera a todos sin repetir números.
/// </summary>
public partial class ServicioPasajeroServicioTests
{
    /// <summary>Crea la ruta con cuatro pasajeros (ids 1 a 4) con los órdenes indicados.</summary>
    private static async Task<(ServicioPasajeroRepositorioFalso Repositorio, Servicio Ruta)> RutaConCuatroPasajerosAsync(params int[] ordenes)
    {
        var ruta = CrearServicio(CrearJornada());
        var repositorio = new ServicioPasajeroRepositorioFalso();
        for (var i = 0; i < ordenes.Length; i++)
        {
            await repositorio.AgregarAsync(new ServicioPasajero
            {
                ServicioId = ruta.ServicioId, EmpleadoId = i + 1, ProgramacionTransporteId = i + 1, Orden = ordenes[i],
                Estado = EstadoServicioPasajero.PROGRAMADO, DireccionRecogida = "Calle 1"
            });
        }

        return (repositorio, ruta);
    }

    private static async Task<List<int>> IdsEnOrdenAsync(ServicioPasajeroRepositorioFalso repositorio, Servicio ruta)
        => (await repositorio.ObtenerPorServicioAsync(ruta.ServicioId)).OrderBy(p => p.Orden).Select(p => p.ServicioPasajeroId).ToList();

    [Fact]
    public async Task ReordenarAsync_SubeAlPasajeroYCorreALosDemas()
    {
        var (repositorio, ruta) = await RutaConCuatroPasajerosAsync(1, 2, 3, 4);

        await CrearServicioPasajeroServicio(repositorio, ruta).ReordenarAsync(EmpresaId, 4, new ReordenarServicioPasajeroDto { NuevoOrden = 2 });

        Assert.Equal(new List<int> { 1, 4, 2, 3 }, await IdsEnOrdenAsync(repositorio, ruta));
        Assert.Equal(new List<int> { 1, 2, 3, 4 }, (await repositorio.ObtenerPorServicioAsync(ruta.ServicioId)).Select(p => p.Orden).OrderBy(o => o).ToList());
    }

    [Fact]
    public async Task ReordenarAsync_BajaAlPasajeroHastaElFinal_AunquePidaUnaPosicionMayor()
    {
        var (repositorio, ruta) = await RutaConCuatroPasajerosAsync(1, 2, 3, 4);

        await CrearServicioPasajeroServicio(repositorio, ruta).ReordenarAsync(EmpresaId, 1, new ReordenarServicioPasajeroDto { NuevoOrden = 9 });

        Assert.Equal(new List<int> { 2, 3, 4, 1 }, await IdsEnOrdenAsync(repositorio, ruta));
    }

    [Fact]
    public async Task ReordenarAsync_CorrigeOrdenesRepetidos()
    {
        var (repositorio, ruta) = await RutaConCuatroPasajerosAsync(1, 2, 2, 3);

        await CrearServicioPasajeroServicio(repositorio, ruta).ReordenarAsync(EmpresaId, 1, new ReordenarServicioPasajeroDto { NuevoOrden = 1 });

        Assert.Equal(new List<int> { 1, 2, 3, 4 }, (await repositorio.ObtenerPorServicioAsync(ruta.ServicioId)).Select(p => p.Orden).OrderBy(o => o).ToList());
    }
}
