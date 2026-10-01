namespace LFMova.Application.DTOs.Cuenta;

/// <summary>Datos que el usuario autenticado puede modificar de su propia cuenta.</summary>
public class ActualizarCuentaDto
{
    /// <summary>Nombre completo de la persona.</summary>
    public string NombreCompleto { get; set; } = string.Empty;

    /// <summary>Teléfono de contacto.</summary>
    public string Telefono { get; set; } = string.Empty;
}
