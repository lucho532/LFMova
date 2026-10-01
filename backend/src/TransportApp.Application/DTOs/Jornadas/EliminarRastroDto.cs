namespace TransportApp.Application.DTOs.Jornadas;

/// <summary>Resultado de eliminar todo rastro de programación de una jornada, para volver a importar desde cero.</summary>
public class EliminarRastroDto
{
    /// <summary>Cuántos servicios (rutas) se eliminaron.</summary>
    public int ServiciosEliminados { get; set; }

    /// <summary>Cuántos pasajeros de esos servicios se eliminaron junto con ellos.</summary>
    public int PasajerosEliminados { get; set; }

    /// <summary>Cuántas programaciones de transporte (filas del Excel ya procesadas) se eliminaron.</summary>
    public int ProgramacionesEliminadas { get; set; }

    /// <summary>Si la jornada misma quedó vacía y también se eliminó.</summary>
    public bool JornadaEliminada { get; set; }
}
