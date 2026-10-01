using LFMova.Application.DTOs.Chat;
using LFMova.Domain.Entities;

namespace LFMova.Application.Mappers;

/// <summary>
/// Convierte entre <see cref="Mensaje"/> y su DTO. No contiene reglas de
/// negocio ni realiza consultas de persistencia.
/// </summary>
public static class MensajeMapper
{
    /// <summary>Convierte una entidad <see cref="Mensaje"/> en su DTO público.</summary>
    public static MensajeDto AMensajeDto(Mensaje mensaje) => new()
    {
        MensajeId = mensaje.MensajeId,
        UsuarioId = mensaje.UsuarioId,
        Contenido = mensaje.Contenido,
        FechaHora = mensaje.FechaHora
    };
}
