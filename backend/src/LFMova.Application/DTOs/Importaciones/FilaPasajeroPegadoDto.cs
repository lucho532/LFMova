namespace LFMova.Application.DTOs.Importaciones;

/// <summary>Un pasajero de una fila pegada, ya separado en sus campos individuales.</summary>
public class FilaPasajeroPegadoDto
{
    /// <summary>Cédula.</summary>
    public string Cedula { get; set; } = string.Empty;

    /// <summary>Nombre completo.</summary>
    public string NombreCompleto { get; set; } = string.Empty;

    /// <summary>Celular.</summary>
    public string Celular { get; set; } = string.Empty;

    /// <summary>Dirección de recogida o de destino.</summary>
    public string Direccion { get; set; } = string.Empty;

    /// <summary>Barrio.</summary>
    public string Barrio { get; set; } = string.Empty;
}
