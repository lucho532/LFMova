namespace LFMova.Application.DTOs.Estadisticas;

/// <summary>Resumen histórico de la empresa para la pantalla de inicio del coordinador.</summary>
public class ResumenEmpresaDto
{
    /// <summary>Rutas programadas hasta la fecha (todas menos las canceladas).</summary>
    public int RutasProgramadas { get; set; }

    /// <summary>Rutas ya realizadas (finalizadas).</summary>
    public int RutasRealizadas { get; set; }

    /// <summary>Rutas canceladas.</summary>
    public int RutasCanceladas { get; set; }

    /// <summary>Rutas aún por realizar (programadas y no finalizadas).</summary>
    public int RutasPorRealizar { get; set; }

    /// <summary>Acumulado de pasajeros asignados a rutas no canceladas.</summary>
    public int PasajerosProgramados { get; set; }

    /// <summary>Acumulado de pasajeros ya transportados (dejados en su destino).</summary>
    public int PasajerosTransportados { get; set; }

    /// <summary>Conductores vinculados activos.</summary>
    public int Conductores { get; set; }

    /// <summary>Empleados activos.</summary>
    public int Empleados { get; set; }

    /// <summary>Sedes activas.</summary>
    public int Sedes { get; set; }
}

/// <summary>Actividad de un conductor en un rango de fechas.</summary>
public class EstadisticaConductorDto
{
    /// <summary>Identificador del conductor.</summary>
    public int ConductorId { get; set; }

    /// <summary>Cédula.</summary>
    public string Cedula { get; set; } = string.Empty;

    /// <summary>Nombre completo.</summary>
    public string NombreCompleto { get; set; } = string.Empty;

    /// <summary>Teléfono.</summary>
    public string Telefono { get; set; } = string.Empty;

    /// <summary>Sus vehículos activos, con los datos que se muestran en el directorio de conductores.</summary>
    public List<VehiculoResumenDto> Vehiculos { get; set; } = new();

    /// <summary>Rutas programadas (no canceladas) en el rango.</summary>
    public int RutasProgramadas { get; set; }

    /// <summary>Rutas finalizadas en el rango.</summary>
    public int RutasRealizadas { get; set; }

    /// <summary>Pasajeros transportados en el rango.</summary>
    public int PasajerosTransportados { get; set; }
}

/// <summary>Datos de un vehículo para el directorio de conductores (sin depender de VehiculoDto para no arrastrar el conductorId).</summary>
public class VehiculoResumenDto
{
    /// <summary>Placa.</summary>
    public string Placa { get; set; } = string.Empty;

    /// <summary>Marca.</summary>
    public string Marca { get; set; } = string.Empty;

    /// <summary>Modelo.</summary>
    public string Modelo { get; set; } = string.Empty;

    /// <summary>Capacidad de pasajeros.</summary>
    public int Capacidad { get; set; }

    /// <summary>Vigencia del SOAT.</summary>
    public DateOnly? VigenciaSoat { get; set; }

    /// <summary>Vigencia de la técnico-mecánica.</summary>
    public DateOnly? VigenciaTecnomecanica { get; set; }
}

/// <summary>Una ruta (servicio) de un conductor.</summary>
public class RutaConductorDto
{
    /// <summary>Identificador del servicio.</summary>
    public int ServicioId { get; set; }

    /// <summary>Jornada a la que pertenece.</summary>
    public int JornadaId { get; set; }

    /// <summary>Fecha del servicio.</summary>
    public DateOnly Fecha { get; set; }

    /// <summary>Hora programada.</summary>
    public TimeOnly Hora { get; set; }

    /// <summary>Tipo (0 entrada, 1 salida).</summary>
    public int Tipo { get; set; }

    /// <summary>Estado del servicio (entero del enum EstadoServicio).</summary>
    public int Estado { get; set; }

    /// <summary>Nombre de la sede.</summary>
    public string Sede { get; set; } = string.Empty;

    /// <summary>Pasajeros asignados.</summary>
    public int Pasajeros { get; set; }

    /// <summary>Pasajeros transportados.</summary>
    public int PasajerosTransportados { get; set; }
}

/// <summary>Una ruta de la empresa con su horario, unidad y conductor asignados.</summary>
public class RutaEmpresaDto
{
    /// <summary>Identificador del servicio.</summary>
    public int ServicioId { get; set; }

    /// <summary>Jornada a la que pertenece.</summary>
    public int JornadaId { get; set; }

    /// <summary>Fecha del servicio.</summary>
    public DateOnly Fecha { get; set; }

    /// <summary>Hora programada.</summary>
    public TimeOnly Hora { get; set; }

    /// <summary>Tipo (0 entrada, 1 salida).</summary>
    public int Tipo { get; set; }

    /// <summary>Estado del servicio (entero del enum EstadoServicio).</summary>
    public int Estado { get; set; }

    /// <summary>Nombre de la sede.</summary>
    public string Sede { get; set; } = string.Empty;

    /// <summary>Unidad operativa asignada, o <c>null</c> si no tiene.</summary>
    public int? UnidadOperativaId { get; set; }

    /// <summary>Nombre del conductor de la unidad asignada.</summary>
    public string? Conductor { get; set; }

    /// <summary>Placa del vehículo de la unidad asignada.</summary>
    public string? Placa { get; set; }

    /// <summary>Pasajeros asignados.</summary>
    public int Pasajeros { get; set; }

    /// <summary>Pasajeros transportados.</summary>
    public int PasajerosTransportados { get; set; }
}

/// <summary>Un pasajero de una ruta de la empresa.</summary>
public class PasajeroEmpresaDto
{
    /// <summary>Cédula del empleado.</summary>
    public string Cedula { get; set; } = string.Empty;

    /// <summary>Nombre completo.</summary>
    public string NombreCompleto { get; set; } = string.Empty;

    /// <summary>Teléfono.</summary>
    public string Telefono { get; set; } = string.Empty;

    /// <summary>Identificador del servicio.</summary>
    public int ServicioId { get; set; }

    /// <summary>Jornada del servicio.</summary>
    public int JornadaId { get; set; }

    /// <summary>Fecha del servicio.</summary>
    public DateOnly Fecha { get; set; }

    /// <summary>Hora programada.</summary>
    public TimeOnly Hora { get; set; }

    /// <summary>Tipo (0 entrada, 1 salida).</summary>
    public int Tipo { get; set; }

    /// <summary>Nombre de la sede.</summary>
    public string Sede { get; set; } = string.Empty;

    /// <summary>Estado del pasajero (entero del enum EstadoServicioPasajero).</summary>
    public int Estado { get; set; }

    /// <summary>Conductor de la ruta, si tiene unidad asignada.</summary>
    public string? Conductor { get; set; }
}
