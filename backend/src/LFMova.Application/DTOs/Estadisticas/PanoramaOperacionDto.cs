namespace LFMova.Application.DTOs.Estadisticas;

/// <summary>Pasajeros transportados un día, separados por tipo de servicio, para la gráfica de líneas del inicio.</summary>
public class PasajerosDiaDto
{
    /// <summary>Día de los servicios.</summary>
    public DateOnly Fecha { get; set; }

    /// <summary>Pasajeros transportados en servicios de entrada.</summary>
    public int Entradas { get; set; }

    /// <summary>Pasajeros transportados en servicios de salida.</summary>
    public int Salidas { get; set; }
}

/// <summary>Indicadores y serie diaria de un periodo, para el panorama de operación del coordinador.</summary>
public class PanoramaOperacionDto
{
    /// <summary>Primer día del periodo.</summary>
    public DateOnly Desde { get; set; }

    /// <summary>Último día del periodo.</summary>
    public DateOnly Hasta { get; set; }

    /// <summary>Un elemento por cada día del periodo, también los que no tuvieron servicios.</summary>
    public List<PasajerosDiaDto> Serie { get; set; } = new();

    /// <summary>Pasajeros transportados en el periodo.</summary>
    public int EmpleadosTransportados { get; set; }

    /// <summary>Variación porcentual frente al periodo anterior de igual duración; <c>null</c> si ese no tuvo pasajeros.</summary>
    public double? VariacionEmpleadosTransportados { get; set; }

    /// <summary>Servicios finalizados en el periodo.</summary>
    public int ServiciosRealizados { get; set; }

    /// <summary>De los servicios realizados, los de entrada.</summary>
    public int ServiciosEntrada { get; set; }

    /// <summary>De los servicios realizados, los de salida.</summary>
    public int ServiciosSalida { get; set; }

    /// <summary>Conductores distintos con al menos un servicio publicado, en curso o finalizado en el periodo.</summary>
    public int ConductoresEnOperacion { get; set; }

    /// <summary>Porcentaje de pasajeros de servicios finalizados que asistieron (0 a 100).</summary>
    public double PromedioAsistencia { get; set; }
}
