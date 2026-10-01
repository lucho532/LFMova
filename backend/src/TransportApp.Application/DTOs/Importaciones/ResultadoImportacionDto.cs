namespace TransportApp.Application.DTOs.Importaciones;

/// <summary>Resumen de una importación ya ejecutada.</summary>
public class ResultadoImportacionDto
{
    /// <summary>Jornada donde quedaron los servicios (existente o creada).</summary>
    public int JornadaId { get; set; }

    /// <summary>Servicio (ruta) donde quedó el pasajero, cuando la operación afecta a uno solo (alta manual).</summary>
    public int? ServicioId { get; set; }

    /// <summary>Servicios creados (los que ya existían se reutilizan).</summary>
    public int ServiciosCreados { get; set; }

    /// <summary>Pasajeros asignados a servicios.</summary>
    public int PasajerosAsignados { get; set; }

    /// <summary>Empleados creados (sus cuentas quedan pendientes de que la persona se registre).</summary>
    public int EmpleadosCreados { get; set; }

    /// <summary>Empleados vinculados a esta empresa que ya tenían cuenta o pertenecían a otra empresa.</summary>
    public int EmpleadosVinculados { get; set; }

    /// <summary>Programaciones creadas.</summary>
    public int ProgramacionesCreadas { get; set; }

    /// <summary>Filas omitidas por ya estar importadas anteriormente.</summary>
    public int FilasOmitidas { get; set; }

    /// <summary>Advertencias registradas durante la importación.</summary>
    public List<string> Advertencias { get; set; } = new();

    /// <summary>Barrios del archivo que no coinciden con el de ninguna Zona activa (ver <see cref="VistaPreviaImportacionDto.BarriosSinZona"/>).</summary>
    public List<string> BarriosSinZona { get; set; } = new();
}
