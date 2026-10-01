using LFMova.Application.DTOs.Sedes;
using LFMova.Application.Interfaces;
using LFMova.Application.Mappers;
using LFMova.Domain.Entities;
using LFMova.Domain.Rules;

namespace LFMova.Application.Implementations;

/// <summary>
/// Implementa los casos de uso de administración de sedes de una empresa.
/// Aplica <see cref="ReglasMultiempresa"/> para garantizar que una sede
/// solamente se consulte o modifique dentro del contexto de su propia
/// empresa. No accede directamente a Entity Framework Core.
/// </summary>
public class SedeServicio : ISedeServicio
{
    private readonly ISedeRepositorio _sedeRepositorio;

    /// <summary>Crea el servicio con su repositorio.</summary>
    public SedeServicio(ISedeRepositorio sedeRepositorio)
    {
        _sedeRepositorio = sedeRepositorio;
    }

    /// <inheritdoc />
    public async Task<SedeDto> CrearAsync(int empresaId, CrearSedeDto datos)
    {
        var sede = new Sede
        {
            EmpresaId = empresaId,
            Nombre = datos.Nombre,
            Direccion = datos.Direccion,
            Ciudad = datos.Ciudad,
            Barrio = datos.Barrio,
            Latitud = datos.Latitud,
            Longitud = datos.Longitud,
            Activa = true
        };

        await _sedeRepositorio.AgregarAsync(sede);
        await _sedeRepositorio.GuardarCambiosAsync();

        return SedeMapper.ASedeDto(sede);
    }

    /// <inheritdoc />
    public async Task<SedeDto?> ObtenerPorIdAsync(int empresaId, int sedeId)
    {
        var sede = await _sedeRepositorio.ObtenerPorIdAsync(sedeId);
        if (sede is null || !ReglasMultiempresa.SedePerteneceAEmpresa(sede, empresaId))
        {
            return null;
        }

        return SedeMapper.ASedeDto(sede);
    }

    /// <inheritdoc />
    public async Task<List<SedeDto>> ObtenerPorEmpresaAsync(int empresaId)
    {
        var sedes = await _sedeRepositorio.ObtenerPorEmpresaAsync(empresaId);
        return sedes.Select(SedeMapper.ASedeDto).ToList();
    }

    /// <inheritdoc />
    public async Task ActualizarAsync(int empresaId, int sedeId, ActualizarSedeDto datos)
    {
        var sede = await ObtenerSedeDeLaEmpresaOFallarAsync(empresaId, sedeId);

        sede.Nombre = datos.Nombre;
        sede.Direccion = datos.Direccion;
        sede.Ciudad = datos.Ciudad;
        sede.Barrio = datos.Barrio;
        sede.Latitud = datos.Latitud;
        sede.Longitud = datos.Longitud;

        await _sedeRepositorio.GuardarCambiosAsync();
    }

    /// <inheritdoc />
    public async Task ActivarAsync(int empresaId, int sedeId)
    {
        var sede = await ObtenerSedeDeLaEmpresaOFallarAsync(empresaId, sedeId);
        sede.Activa = true;
        await _sedeRepositorio.GuardarCambiosAsync();
    }

    /// <inheritdoc />
    public async Task DesactivarAsync(int empresaId, int sedeId)
    {
        var sede = await ObtenerSedeDeLaEmpresaOFallarAsync(empresaId, sedeId);
        sede.Activa = false;
        await _sedeRepositorio.GuardarCambiosAsync();
    }

    private async Task<Sede> ObtenerSedeDeLaEmpresaOFallarAsync(int empresaId, int sedeId)
    {
        var sede = await _sedeRepositorio.ObtenerPorIdAsync(sedeId);
        if (sede is null || !ReglasMultiempresa.SedePerteneceAEmpresa(sede, empresaId))
        {
            throw new InvalidOperationException("La sede indicada no existe en esta empresa.");
        }

        return sede;
    }
}
