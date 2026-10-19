using LFMova.Application.Interfaces;
using LFMova.Application.Utils;

namespace LFMova.Application.Implementations.Importaciones;

/// <summary>Unidad operativa encontrada para una placa o número interno, con el nombre de su conductor, la capacidad del vehículo y el número interno de la empresa (si tiene).</summary>
public sealed record UnidadPorPlaca(int UnidadOperativaId, string Conductor, int Capacidad, string? NumeroInterno);

/// <summary>
/// Encuentra la unidad operativa (conductor + vehículo) que corresponde a la placa que trae un archivo,
/// entre las unidades activas de los conductores con vinculación activa a la empresa. También reconoce el
/// número interno que la empresa le asignó al vehículo (por ejemplo "103"); si coincide con la placa de otro
/// vehículo, gana la placa. Se compara sin mayúsculas, espacios ni guiones. No asigna la unidad a ningún
/// servicio ni inventa conductores: si nada coincide, devuelve <c>null</c>.
/// </summary>
public class ResolutorUnidadPorPlaca
{
    private readonly IConductorRepositorio _conductorRepositorio;
    private readonly IUnidadOperativaRepositorio _unidadRepositorio;
    private readonly IVehiculoRepositorio _vehiculoRepositorio;
    private readonly INumeroInternoUnidadRepositorio _numeroInternoRepositorio;

    /// <summary>Crea el colaborador con sus dependencias.</summary>
    public ResolutorUnidadPorPlaca(
        IConductorRepositorio conductorRepositorio, IUnidadOperativaRepositorio unidadRepositorio, IVehiculoRepositorio vehiculoRepositorio, INumeroInternoUnidadRepositorio numeroInternoRepositorio)
    {
        _numeroInternoRepositorio = numeroInternoRepositorio;
        _conductorRepositorio = conductorRepositorio;
        _unidadRepositorio = unidadRepositorio;
        _vehiculoRepositorio = vehiculoRepositorio;
    }

    /// <summary>Carga una vez las unidades de la empresa, indexadas por placa y por número interno normalizados.</summary>
    public async Task<Dictionary<string, UnidadPorPlaca>> CargarAsync(int empresaId)
    {
        var porPlaca = new Dictionary<string, UnidadPorPlaca>();
        var numeros = (await _numeroInternoRepositorio.ObtenerPorEmpresaAsync(empresaId)).ToDictionary(n => n.UnidadOperativaId, n => n.Numero);
        var porNumero = new Dictionary<string, UnidadPorPlaca>();
        var conductores = await _conductorRepositorio.ObtenerPorEmpresaAsync(empresaId);
        foreach (var conductor in conductores.Where(c => c.Activo && c.VinculacionesConductorEmpresa.Any(v => v.EmpresaId == empresaId && v.Activa)))
        {
            foreach (var unidad in (await _unidadRepositorio.ObtenerPorConductorAsync(conductor.ConductorId)).Where(u => u.Activa))
            {
                var vehiculo = await _vehiculoRepositorio.ObtenerPorIdAsync(unidad.VehiculoId);
                if (vehiculo is { Activo: true })
                {
                    var numeroInterno = numeros.GetValueOrDefault(unidad.UnidadOperativaId);
                    var encontrada = new UnidadPorPlaca(unidad.UnidadOperativaId, conductor.NombreCompleto, vehiculo.Capacidad, numeroInterno);
                    porPlaca.TryAdd(NormalizarPlaca(vehiculo.Placa), encontrada);
                    if (numeroInterno is not null)
                    {
                        porNumero.TryAdd(NormalizarPlaca(numeroInterno), encontrada);
                    }
                }
            }
        }

        foreach (var (clave, unidad) in porNumero)
        {
            porPlaca.TryAdd(clave, unidad);
        }

        return porPlaca;
    }

    /// <summary>Lleva una placa a su forma comparable (mayúsculas, sin espacios ni guiones).</summary>
    public static string NormalizarPlaca(string placa) => new(TextoNormalizador.Normalizar(placa).Where(char.IsLetterOrDigit).ToArray());
}
