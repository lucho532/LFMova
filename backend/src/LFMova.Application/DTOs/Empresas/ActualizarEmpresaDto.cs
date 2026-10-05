namespace LFMova.Application.DTOs.Empresas;

/// <summary>
/// Datos de una empresa que el administrador de plataforma puede corregir
/// después de crearla. No incluye su estado (se activa o desactiva aparte) ni
/// sus coordinadores.
/// </summary>
public class ActualizarEmpresaDto
{
    /// <summary>Nombre de la empresa.</summary>
    public string Nombre { get; set; } = string.Empty;

    /// <summary>Identificador fiscal; no puede repetirse entre empresas.</summary>
    public string Cif { get; set; } = string.Empty;

    /// <summary>Dirección de la empresa.</summary>
    public string Direccion { get; set; } = string.Empty;
}
