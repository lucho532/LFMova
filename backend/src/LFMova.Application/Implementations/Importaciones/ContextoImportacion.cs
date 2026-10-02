using LFMova.Application.DTOs.Importaciones;
using LFMova.Application.DTOs.Jornadas;
using LFMova.Application.DTOs.Programaciones;
using LFMova.Application.DTOs.Servicios;
using LFMova.Domain.Entities;
using LFMova.Domain.Enums;

namespace LFMova.Application.Implementations.Importaciones;

/// <summary>
/// Datos que se van acumulando mientras se ejecuta una importación: lo que ya existía en la empresa y
/// lo que la propia importación va creando. Vive solo durante esa ejecución; no contiene lógica ni se
/// guarda en la base de datos.
/// </summary>
public sealed class ContextoImportacion
{
    /// <summary>Empresa para la que se importa.</summary>
    public required int EmpresaId { get; init; }

    /// <summary>Resultado que se le devuelve al coordinador, con los contadores y advertencias de la importación.</summary>
    public required ResultadoImportacionDto Resultado { get; init; }

    /// <summary>Jornadas que la empresa ya tenía antes de importar.</summary>
    public required List<JornadaDto> JornadasExistentes { get; init; }

    /// <summary>Programaciones de la empresa, incluidas las que va creando esta importación.</summary>
    public required List<ProgramacionDto> Programaciones { get; init; }

    /// <summary>Sedes activas de la empresa, incluidas las que va creando esta importación.</summary>
    public required List<Sede> Sedes { get; init; }

    /// <summary>Unidades entre las que se reparte (vacía si no se pidió repartir).</summary>
    public required List<UnidadDisponible> Unidades { get; init; }

    /// <summary>Zonas de la empresa para el reparto (vacío si no se pidió repartir).</summary>
    public required MapaZonas MapaZonas { get; init; }

    /// <summary>Una jornada por fecha operativa ya usada en esta importación, con sus servicios.</summary>
    public Dictionary<DateOnly, (JornadaDto Jornada, List<ServicioDto> Servicios)> JornadasCargadas { get; } = new();

    /// <summary>
    /// Rutas que ya ocupan a cada unidad (existentes y las que va creando esta importación), para no darle
    /// a un conductor dos rutas que se crucen (ver ReglasUnidadOperativa.SeCruzan: a la misma hora solo
    /// puede hacer una entrada y una salida de la misma sede).
    /// </summary>
    public List<(int UnidadOperativaId, DateOnly Fecha, TimeOnly Hora, TipoServicio Tipo, int SedeId)> Ocupaciones { get; } = new();

    /// <summary>Servicios que deben avanzar de estado al terminar: los creados ahora y los que habían quedado en Borrador.</summary>
    public List<ServicioDto> ServiciosNuevos { get; } = new();
}
