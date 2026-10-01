using TransportApp.Application.DTOs.Empleados;
using TransportApp.Domain.Entities;

namespace TransportApp.Application.Mappers;

/// <summary>
/// Convierte entre <see cref="Empleado"/> y su DTO. No contiene reglas de
/// negocio ni realiza consultas de persistencia.
/// </summary>
public static class EmpleadoMapper
{
    /// <summary>
    /// Convierte una entidad <see cref="Empleado"/> en su DTO público.
    /// Requiere que <see cref="Empleado.Usuario"/> esté cargado.
    /// </summary>
    public static EmpleadoDto AEmpleadoDto(Empleado empleado) => new()
    {
        EmpleadoId = empleado.EmpleadoId,
        UsuarioId = empleado.UsuarioId,
        EmpresaId = empleado.EmpresaId,
        Cedula = empleado.Usuario?.Cedula ?? string.Empty,
        Email = empleado.Usuario?.Email,
        NombreCompleto = empleado.NombreCompleto,
        Telefono = empleado.Telefono,
        Direccion = empleado.Direccion,
        Barrio = empleado.Barrio,
        Activo = empleado.Activo
    };
}
