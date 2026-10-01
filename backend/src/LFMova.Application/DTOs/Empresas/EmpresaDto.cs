namespace LFMova.Application.DTOs.Empresas;

/// <summary>Representación pública de una empresa.</summary>
public class EmpresaDto
{
    /// <summary>Identificador único de la empresa.</summary>
    public int EmpresaId { get; set; }

    /// <summary>Nombre de la empresa.</summary>
    public string Nombre { get; set; } = string.Empty;

    /// <summary>Identificador fiscal de la empresa (CIF/NIT).</summary>
    public string Cif { get; set; } = string.Empty;

    /// <summary>Dirección física de la empresa.</summary>
    public string Direccion { get; set; } = string.Empty;

    /// <summary>
    /// Cédula del primer coordinador activo de la empresa, o <c>null</c> si
    /// no tiene ninguno activo. Solo se completa en el listado general de
    /// empresas.
    /// </summary>
    public string? CoordinadorPrincipal { get; set; }

    /// <summary>Nombre completo del coordinador principal (ver <see cref="CoordinadorPrincipal"/>).</summary>
    public string? CoordinadorPrincipalNombre { get; set; }

    /// <summary>Indica si la empresa está activa.</summary>
    public bool Activa { get; set; }
}
