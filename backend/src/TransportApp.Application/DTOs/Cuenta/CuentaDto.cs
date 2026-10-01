namespace TransportApp.Application.DTOs.Cuenta;

/// <summary>Datos de la propia cuenta del usuario autenticado.</summary>
public class CuentaDto
{
    /// <summary>Cédula de la persona. No se puede modificar.</summary>
    public string Cedula { get; set; } = string.Empty;

    /// <summary>Nombre completo de la persona.</summary>
    public string NombreCompleto { get; set; } = string.Empty;

    /// <summary>Correo electrónico de la cuenta. No se puede modificar desde aquí: cambiarlo exigiría volver a confirmarlo.</summary>
    public string? Email { get; set; }

    /// <summary>Teléfono de contacto.</summary>
    public string Telefono { get; set; } = string.Empty;

    /// <summary>
    /// Nombres de las empresas a las que pertenece la persona (como
    /// coordinador, conductor vinculado o empleado), sin repetir. Vacía para
    /// quien no pertenece a ninguna, como el administrador de plataforma.
    /// </summary>
    public List<string> Empresas { get; set; } = new();
}
