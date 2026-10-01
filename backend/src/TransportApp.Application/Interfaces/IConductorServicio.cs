using TransportApp.Application.DTOs.Conductores;
using TransportApp.Application.DTOs.Servicios;

namespace TransportApp.Application.Interfaces;

/// <summary>
/// Define los casos de uso de administración de conductores y sus
/// vinculaciones con empresas. No decide autorización: eso se verifica en la
/// capa de Api antes de invocar estos métodos.
/// </summary>
public interface IConductorServicio
{
    /// <summary>
    /// Registra un nuevo conductor (creando su <c>Usuario</c> si no existe) y
    /// lo vincula con la empresa indicada. Falla si la cédula ya corresponde
    /// a un conductor existente; en ese caso debe usarse
    /// <see cref="VincularAsync"/>.
    /// </summary>
    Task<ConductorDto> CrearAsync(int empresaId, CrearConductorDto datos);

    /// <summary>
    /// Vincula con la empresa indicada un conductor ya existente
    /// (identificado por su cédula), sin duplicar su registro. Falla si el
    /// conductor no existe o si ya tiene una vinculación con esa empresa.
    /// </summary>
    Task<VinculacionConductorEmpresaDto> VincularAsync(int empresaId, VincularConductorDto datos);

    /// <summary>Obtiene un conductor por su identificador, o <c>null</c> si no existe.</summary>
    Task<ConductorDto?> ObtenerPorIdAsync(int conductorId);

    /// <summary>Obtiene los conductores vinculados a la empresa indicada.</summary>
    Task<List<ConductorDto>> ObtenerPorEmpresaAsync(int empresaId);

    /// <summary>Activa la vinculación de un conductor con una empresa.</summary>
    Task ActivarVinculacionAsync(int empresaId, int conductorId);

    /// <summary>Desactiva la vinculación de un conductor con una empresa.</summary>
    Task DesactivarVinculacionAsync(int empresaId, int conductorId);

    /// <summary>
    /// Obtiene los servicios asignados a las unidades operativas del
    /// conductor asociado al usuario indicado (en cualquier empresa con la
    /// que tenga vinculación), más recientes primero. Falla si el usuario
    /// autenticado no tiene un perfil de conductor.
    /// </summary>
    Task<List<ServicioDto>> ObtenerServiciosPropiosAsync(int usuarioId);

    /// <summary>
    /// Obtiene las unidades operativas del conductor asociado al usuario
    /// indicado, cada una con los datos completos de su vehículo. Falla si
    /// el usuario no tiene un perfil de conductor.
    /// </summary>
    Task<List<UnidadDeTrabajoDto>> ObtenerUnidadesPropiasAsync(int usuarioId);
}
