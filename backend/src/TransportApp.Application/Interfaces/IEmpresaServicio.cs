using TransportApp.Application.DTOs.Empresas;

namespace TransportApp.Application.Interfaces;

/// <summary>
/// Define los casos de uso de administración de empresas: creación,
/// consulta y activación/desactivación. No decide autorización: eso se
/// verifica antes de invocar estos métodos (rol y contexto de empresa).
/// </summary>
public interface IEmpresaServicio
{
    /// <summary>
    /// Crea una nueva empresa junto con su primer coordinador (usuario y
    /// contraseña, definida por el administrador de plataforma que ejecuta
    /// la operación). Rechaza la operación si el CIF ya está registrado, o
    /// si la cédula del coordinador pertenece a un usuario que ya tiene una
    /// contraseña establecida (no se sobrescriben credenciales existentes).
    /// </summary>
    Task<EmpresaDto> CrearAsync(CrearEmpresaDto datos);

    /// <summary>Obtiene una empresa por su identificador, o <c>null</c> si no existe.</summary>
    Task<EmpresaDto?> ObtenerPorIdAsync(int empresaId);

    /// <summary>Obtiene todas las empresas registradas.</summary>
    Task<List<EmpresaDto>> ObtenerTodasAsync();

    /// <summary>Activa una empresa existente.</summary>
    Task ActivarAsync(int empresaId);

    /// <summary>Desactiva una empresa existente.</summary>
    Task DesactivarAsync(int empresaId);
}
