using TransportApp.Application.DTOs.Zonas;

namespace TransportApp.Application.Interfaces;

/// <summary>
/// Define los casos de uso de administración de barreras geográficas entre
/// barrios de una empresa. No decide autorización: eso se verifica en la
/// capa de Api antes de invocar estos métodos.
/// </summary>
public interface IBarreraGeograficaServicio
{
    /// <summary>Declara una nueva barrera geográfica entre dos barrios de la empresa indicada.</summary>
    Task<BarreraGeograficaDto> CrearAsync(int empresaId, CrearBarreraGeograficaDto datos);

    /// <summary>Obtiene todas las barreras geográficas de la empresa indicada.</summary>
    Task<List<BarreraGeograficaDto>> ObtenerPorEmpresaAsync(int empresaId);

    /// <summary>Elimina una barrera geográfica de la empresa indicada.</summary>
    Task EliminarAsync(int empresaId, int barreraGeograficaId);
}
