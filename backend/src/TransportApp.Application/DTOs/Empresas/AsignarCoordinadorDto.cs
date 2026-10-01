namespace TransportApp.Application.DTOs.Empresas;

/// <summary>
/// Datos de entrada para asignar el rol <c>COORDINADOR</c> de una empresa a
/// un usuario identificado por su cédula. Si el usuario no existe todavía,
/// se crea con la cuenta pendiente de activación y se le envía una
/// invitación por correo para que establezca su contraseña; en ese caso
/// <see cref="NombreCoordinador"/>, <see cref="TelefonoCoordinador"/> y
/// <see cref="CorreoCoordinador"/> son obligatorios. Si el usuario ya existe,
/// esos tres campos se ignoran.
/// </summary>
public class AsignarCoordinadorDto
{
    /// <summary>Cédula de la persona a la que se asigna el rol.</summary>
    public string Cedula { get; set; } = string.Empty;

    /// <summary>Nombre completo, solo necesario si la persona todavía no tiene cuenta.</summary>
    public string NombreCoordinador { get; set; } = string.Empty;

    /// <summary>Teléfono de contacto, solo necesario si la persona todavía no tiene cuenta.</summary>
    public string TelefonoCoordinador { get; set; } = string.Empty;

    /// <summary>Correo electrónico, solo necesario si la persona todavía no tiene cuenta.</summary>
    public string CorreoCoordinador { get; set; } = string.Empty;
}
