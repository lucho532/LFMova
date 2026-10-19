namespace LFMova.Application.DTOs.Jornadas;

/// <summary>Resultado de deshacer el reparto automático de una jornada.</summary>
public class DeshacerRepartoDto
{
    /// <summary>Cuántos servicios (rutas) se eliminaron.</summary>
    public int ServiciosEliminados { get; set; }

    /// <summary>Cuántos pasajeros quedaron sin asignar, listos para repartirse de nuevo.</summary>
    public int PasajerosLiberados { get; set; }
}
