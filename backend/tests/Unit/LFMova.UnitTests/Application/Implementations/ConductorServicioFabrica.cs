using LFMova.Application.Implementations;
using LFMova.Application.Implementations.Conductores;
using LFMova.Application.Interfaces;

namespace LFMova.UnitTests.Application.Implementations;

/// <summary>
/// Arma un <see cref="ConductorServicio"/> real con su colaborador a partir de los repositorios que
/// cada prueba decide (normalmente falsos en memoria), igual que lo haría el contenedor de
/// dependencias. No contiene aserciones ni datos de prueba.
/// </summary>
internal static class ConductorServicioFabrica
{
    /// <summary>Crea el servicio conectando su colaborador con las dependencias indicadas.</summary>
    public static ConductorServicio Crear(
        IConductorRepositorio conductorRepositorio,
        IUsuarioRepositorio usuarioRepositorio,
        IEmpresaRepositorio empresaRepositorio,
        IUsuarioRolRepositorio usuarioRolRepositorio,
        IUnidadOperativaRepositorio unidadOperativaRepositorio,
        IServicioRepositorio servicioRepositorio,
        IVehiculoRepositorio vehiculoRepositorio)
        => new(
            conductorRepositorio, usuarioRepositorio, empresaRepositorio, unidadOperativaRepositorio, servicioRepositorio, vehiculoRepositorio,
            new RegistroConductor(
                conductorRepositorio, usuarioRepositorio, empresaRepositorio, usuarioRolRepositorio, unidadOperativaRepositorio, vehiculoRepositorio));
}
