using LFMova.Domain.Enums;

namespace LFMova.Application.DTOs.Servicios;

/// <summary>Datos necesarios para solicitar un cambio de estado de un servicio.</summary>
public class CambiarEstadoServicioDto
{
    /// <summary>Estado al que se solicita mover el servicio.</summary>
    public EstadoServicio NuevoEstado { get; set; }
}
