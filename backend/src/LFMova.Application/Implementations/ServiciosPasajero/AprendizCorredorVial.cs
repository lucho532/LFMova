using LFMova.Application.Interfaces;
using LFMova.Application.Utils;
using LFMova.Domain.Entities;
using LFMova.Domain.Rules;

namespace LFMova.Application.Implementations.ServiciosPasajero;

/// <summary>
/// Aprende qué zonas pueden compartir vehículo a partir de lo que hace el coordinador al mover
/// pasajeros entre rutas, creando o ampliando corredores viales. Nunca mueve pasajeros ni reparte
/// rutas: solo deja registrada esa relación para las próximas importaciones.
/// </summary>
public class AprendizCorredorVial
{
    /// <summary>Tope de zonas que un corredor vial puede acumular por aprendizaje automático: pasado esto, agrandarlo es una decisión manual del coordinador desde Zonas, no algo que un solo movimiento deba decidir por sí solo.</summary>
    private const int MaximoZonasPorCorredorAprendido = 4;

    private readonly IServicioPasajeroRepositorio _servicioPasajeroRepositorio;
    private readonly IEmpleadoRepositorio _empleadoRepositorio;
    private readonly IZonaRepositorio _zonaRepositorio;
    private readonly ICorredorVialRepositorio _corredorVialRepositorio;
    private readonly IBarreraGeograficaRepositorio _barreraGeograficaRepositorio;

    /// <summary>Crea el colaborador con sus repositorios.</summary>
    public AprendizCorredorVial(
        IServicioPasajeroRepositorio servicioPasajeroRepositorio,
        IEmpleadoRepositorio empleadoRepositorio,
        IZonaRepositorio zonaRepositorio,
        ICorredorVialRepositorio corredorVialRepositorio,
        IBarreraGeograficaRepositorio barreraGeograficaRepositorio)
    {
        _servicioPasajeroRepositorio = servicioPasajeroRepositorio;
        _empleadoRepositorio = empleadoRepositorio;
        _zonaRepositorio = zonaRepositorio;
        _corredorVialRepositorio = corredorVialRepositorio;
        _barreraGeograficaRepositorio = barreraGeograficaRepositorio;
    }

    /// <summary>
    /// Cuando el coordinador mueve un pasajero de una ruta a otra, eso es información real sobre qué
    /// barrios pueden compartir vehículo: si la zona del barrio del pasajero movido y las de los demás
    /// pasajeros de la ruta destino son distintas y todavía no comparten corredor vial, se crea (o se
    /// amplía) automáticamente un <see cref="CorredorVial"/> que las une, para que las próximas
    /// importaciones ya las repartan juntas sin que el coordinador tenga que declararlo a mano. Nunca
    /// une zonas cuyos barrios tengan una <see cref="BarreraGeografica"/> declarada entre sí, nunca
    /// fusiona dos corredores ya existentes y distintos entre sí, y un corredor deja de crecer por
    /// aprendizaje automático al llegar a <see cref="MaximoZonasPorCorredorAprendido"/> zonas (esas
    /// decisiones sí las toma el coordinador a mano desde Zonas). Si el barrio de algún pasajero no está
    /// clasificado en ninguna zona, simplemente no hay nada que aprender de él.
    /// </summary>
    public async Task AprenderAsync(int empresaId, ServicioPasajero pasajeroMovido, Servicio destino)
    {
        var empleadoMovido = await _empleadoRepositorio.ObtenerPorIdAsync(pasajeroMovido.EmpleadoId);
        if (empleadoMovido is null)
        {
            return;
        }

        var zonas = (await _zonaRepositorio.ObtenerPorEmpresaAsync(empresaId)).Where(z => z.Activa).ToList();
        var zonaMovido = ResolverZonaPorBarrio(empleadoMovido.Barrio, zonas);
        if (zonaMovido is null)
        {
            return;
        }

        var pasajerosDestino = await _servicioPasajeroRepositorio.ObtenerPorServicioAsync(destino.ServicioId);
        var barreras = await _barreraGeograficaRepositorio.ObtenerPorEmpresaAsync(empresaId);
        var zonasOtrasYaVinculadas = new HashSet<int>();

        foreach (var pasajeroDestino in pasajerosDestino)
        {
            if (pasajeroDestino.ServicioPasajeroId == pasajeroMovido.ServicioPasajeroId)
            {
                continue;
            }

            var empleadoDestino = await _empleadoRepositorio.ObtenerPorIdAsync(pasajeroDestino.EmpleadoId);
            var zonaOtra = empleadoDestino is null ? null : ResolverZonaPorBarrio(empleadoDestino.Barrio, zonas);
            if (zonaOtra is null || zonaOtra.ZonaId == zonaMovido.ZonaId || !zonasOtrasYaVinculadas.Add(zonaOtra.ZonaId))
            {
                continue;
            }

            await VincularZonasSiCorrespondeAsync(empresaId, zonaMovido, zonaOtra, zonas, barreras);
        }
    }

    private static Zona? ResolverZonaPorBarrio(string barrio, List<Zona> zonas)
    {
        var normalizado = TextoNormalizador.Normalizar(barrio);
        return zonas.FirstOrDefault(z => z.Barrios.Any(b => TextoNormalizador.Normalizar(b) == normalizado));
    }

    private async Task VincularZonasSiCorrespondeAsync(int empresaId, Zona zonaA, Zona zonaB, List<Zona> todasLasZonas, List<BarreraGeografica> barreras)
    {
        if (zonaA.CorredorVialId is not null && zonaA.CorredorVialId == zonaB.CorredorVialId)
        {
            return;
        }

        if (zonaA.CorredorVialId is not null && zonaB.CorredorVialId is not null)
        {
            return;
        }

        var todosLosBarrios = zonaA.Barrios.Concat(zonaB.Barrios).ToList();
        if (ReglasBarrerasGeograficas.BuscarParConBarrera(todosLosBarrios, barreras) is not null)
        {
            return;
        }

        // Un corredor ya aprendido no sigue creciendo sin límite: que dos zonas hayan compartido vehículo
        // una vez no dice nada sobre una tercera zona que solo coincide en que también comparte ESE
        // corredor con una de las dos. Pasado el tope, ampliarlo pasa a ser una decisión manual.
        if (zonaA.CorredorVialId is not null && todasLasZonas.Count(z => z.CorredorVialId == zonaA.CorredorVialId) >= MaximoZonasPorCorredorAprendido)
        {
            return;
        }

        if (zonaB.CorredorVialId is not null && todasLasZonas.Count(z => z.CorredorVialId == zonaB.CorredorVialId) >= MaximoZonasPorCorredorAprendido)
        {
            return;
        }

        if (zonaA.CorredorVialId is not null)
        {
            zonaB.CorredorVialId = zonaA.CorredorVialId;
            await _zonaRepositorio.GuardarCambiosAsync();
            return;
        }

        if (zonaB.CorredorVialId is not null)
        {
            zonaA.CorredorVialId = zonaB.CorredorVialId;
            await _zonaRepositorio.GuardarCambiosAsync();
            return;
        }

        var corredor = new CorredorVial { EmpresaId = empresaId, Nombre = $"{zonaA.Nombre} - {zonaB.Nombre}", Activo = true };
        await _corredorVialRepositorio.AgregarAsync(corredor);
        await _corredorVialRepositorio.GuardarCambiosAsync();

        zonaA.CorredorVialId = corredor.CorredorVialId;
        zonaB.CorredorVialId = corredor.CorredorVialId;
        await _zonaRepositorio.GuardarCambiosAsync();
    }
}
