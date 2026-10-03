namespace LFMova.Application.DTOs.Cuenta;

/// <summary>
/// Datos para que una persona elimine su propia cuenta: su contraseña
/// actual, como confirmación de que es ella quien lo pide. No lleva el
/// identificador de la cuenta: siempre es la de la sesión.
/// </summary>
public class EliminarCuentaDto
{
    /// <summary>Contraseña actual de la cuenta.</summary>
    public string Contrasena { get; set; } = string.Empty;
}
