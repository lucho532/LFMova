namespace TransportApp.Application.DTOs.Empresas;

/// <summary>
/// Datos de entrada para crear una empresa junto con su primer coordinador.
/// </summary>
public class CrearEmpresaDto
{
    /// <summary>Nombre de la empresa.</summary>
    public string Nombre { get; set; } = string.Empty;

    /// <summary>Identificador fiscal de la empresa (CIF/NIT).</summary>
    public string Cif { get; set; } = string.Empty;

    /// <summary>Dirección física de la empresa.</summary>
    public string Direccion { get; set; } = string.Empty;

    /// <summary>Cédula de la persona que será el primer coordinador de la empresa.</summary>
    public string CedulaCoordinador { get; set; } = string.Empty;

    /// <summary>Nombre completo de la persona que será el primer coordinador.</summary>
    public string NombreCoordinador { get; set; } = string.Empty;

    /// <summary>Teléfono de contacto de la persona que será el primer coordinador.</summary>
    public string TelefonoCoordinador { get; set; } = string.Empty;

    /// <summary>
    /// Correo electrónico de la persona que será el primer coordinador. Se le
    /// envía un enlace para que establezca su propia contraseña: el
    /// administrador de plataforma nunca la define ni la conoce.
    /// </summary>
    public string CorreoCoordinador { get; set; } = string.Empty;
}
