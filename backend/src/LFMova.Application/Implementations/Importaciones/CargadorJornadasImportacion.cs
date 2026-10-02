using LFMova.Application.DTOs.Jornadas;
using LFMova.Application.DTOs.Servicios;
using LFMova.Application.Interfaces;
using LFMova.Domain.Enums;

namespace LFMova.Application.Implementations.Importaciones;

/// <summary>
/// Obtiene la jornada de cada fecha operativa que toca una importación (creándola si hace falta) y
/// deja anotado en el contexto lo que esa jornada ya traía: qué unidades están ocupadas y qué
/// servicios habían quedado en Borrador. No crea servicios ni asigna pasajeros.
/// </summary>
public class CargadorJornadasImportacion
{
    private readonly IJornadaServicio _jornadaServicio;
    private readonly IServicioServicio _servicioServicio;

    /// <summary>Crea el colaborador con sus dependencias.</summary>
    public CargadorJornadasImportacion(IJornadaServicio jornadaServicio, IServicioServicio servicioServicio)
    {
        _jornadaServicio = jornadaServicio;
        _servicioServicio = servicioServicio;
    }

    /// <summary>Devuelve la jornada de la fecha con sus servicios, y la deja cargada para el resto de la importación.</summary>
    public async Task<(JornadaDto Jornada, List<ServicioDto> Servicios)> ObtenerAsync(ContextoImportacion contexto, DateOnly fechaJornada)
    {
        if (contexto.JornadasCargadas.TryGetValue(fechaJornada, out var cargada))
        {
            return cargada;
        }

        var jornadaDelDia = contexto.JornadasExistentes.FirstOrDefault(j => j.FechaOperativa == fechaJornada)
            ?? await _jornadaServicio.CrearAsync(contexto.EmpresaId, new CrearJornadaDto { FechaOperativa = fechaJornada });
        var existentes = await _servicioServicio.ObtenerPorJornadaAsync(contexto.EmpresaId, jornadaDelDia.JornadaId);
        // Un servicio finalizado o cancelado ya no ocupa a su conductor: su unidad puede volver a
        // repartirse en una hora que, en la vida real, ya dejó de estar en curso.
        foreach (var servicioExistente in existentes.Where(s => s.UnidadOperativaId is not null && s.Estado is not (EstadoServicio.FINALIZADO or EstadoServicio.CANCELADO)))
        {
            contexto.Ocupaciones.Add((servicioExistente.UnidadOperativaId!.Value, servicioExistente.Fecha, servicioExistente.HoraProgramada, servicioExistente.Tipo, servicioExistente.SedeId));
        }

        // Servicios de importaciones anteriores que quedaron en Borrador (por ejemplo, tras un fallo) también avanzan.
        contexto.ServiciosNuevos.AddRange(existentes.Where(s => s.Estado == EstadoServicio.BORRADOR));

        if (contexto.JornadasCargadas.Count == 0)
        {
            contexto.Resultado.JornadaId = jornadaDelDia.JornadaId;
        }

        return contexto.JornadasCargadas[fechaJornada] = (jornadaDelDia, existentes);
    }
}
