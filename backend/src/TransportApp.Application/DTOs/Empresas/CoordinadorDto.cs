namespace TransportApp.Application.DTOs.Empresas;

/// <summary>Representación pública de un coordinador de una empresa.</summary>
public class CoordinadorDto
{
    /// <summary>Identificador del registro de rol COORDINADOR (usado para activar/desactivar).</summary>
    public int UsuarioRolId { get; set; }

    /// <summary>Cédula de la persona.</summary>
    public string Cedula { get; set; } = string.Empty;

    /// <summary>Nombre completo del coordinador, tomado de su cuenta.</summary>
    public string NombreCompleto { get; set; } = string.Empty;

    /// <summary>Correo electrónico del coordinador, si su cuenta lo tiene.</summary>
    public string? Email { get; set; }

    /// <summary>Teléfono de contacto del coordinador.</summary>
    public string Telefono { get; set; } = string.Empty;

    /// <summary>Indica si este rol de coordinador está activo.</summary>
    public bool Activo { get; set; }
}
