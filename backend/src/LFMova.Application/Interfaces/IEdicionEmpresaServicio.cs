using LFMova.Application.DTOs.Empresas;

namespace LFMova.Application.Interfaces;

/// <summary>
/// Casos de uso del administrador de plataforma para corregir o eliminar una
/// empresa ya creada (decisión del 2026-10-05). No crea empresas ni gestiona
/// sus coordinadores: eso sigue en <see cref="IEmpresaServicio"/>.
/// </summary>
public interface IEdicionEmpresaServicio
{
    /// <summary>
    /// Corrige el nombre, el CIF y la dirección de la empresa. Falla si la
    /// empresa no existe, si falta algún dato o si el CIF ya es de otra.
    /// </summary>
    Task<EmpresaDto> ActualizarAsync(int empresaId, ActualizarEmpresaDto datos);

    /// <summary>
    /// Elimina la empresa por completo, con todo su historial: sedes, zonas,
    /// jornadas, rutas, pasajeros, incidencias, importaciones, invitaciones y
    /// registros de facturación. Las personas conservan su cuenta, ya sin
    /// empresa (los conductores, también su ficha y su vehículo); solo se
    /// borran las cuentas que nunca se activaron. Falla si la empresa no
    /// existe o si tiene una ruta en curso.
    /// </summary>
    Task EliminarAsync(int empresaId);
}
