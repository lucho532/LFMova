namespace TransportApp.Application.DTOs.Invitaciones;

/// <summary>
/// Datos con los que un coordinador invita a una persona a unirse a su
/// empresa. Si la cédula ya tiene una cuenta con correo, la invitación se
/// envía al correo de esa cuenta (no al escrito aquí), para que siempre le
/// llegue a su verdadero dueño.
/// </summary>
public class CrearInvitacionEmpresaDto
{
    /// <summary>Cédula de la persona invitada.</summary>
    public string Cedula { get; set; } = string.Empty;

    /// <summary>Correo al que enviar la invitación cuando la persona todavía no tiene cuenta con correo.</summary>
    public string Correo { get; set; } = string.Empty;
}
