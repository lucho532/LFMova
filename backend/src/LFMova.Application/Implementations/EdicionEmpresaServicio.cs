using LFMova.Application.DTOs.Empresas;
using LFMova.Application.Interfaces;
using LFMova.Application.Mappers;
using LFMova.Application.Validators;

namespace LFMova.Application.Implementations;

/// <summary>
/// Implementa <see cref="IEdicionEmpresaServicio"/>: valida los datos
/// corregidos de una empresa y decide si puede eliminarse, delegando el
/// borrado en <see cref="IEliminacionEmpresaRepositorio"/>. No accede
/// directamente a Entity Framework Core.
/// </summary>
public class EdicionEmpresaServicio : IEdicionEmpresaServicio
{
    private readonly IEmpresaRepositorio _empresaRepositorio;
    private readonly IEliminacionEmpresaRepositorio _eliminacionRepositorio;
    private readonly IAlmacenamientoArchivos _almacenamiento;

    /// <summary>Crea el servicio con sus dependencias.</summary>
    public EdicionEmpresaServicio(
        IEmpresaRepositorio empresaRepositorio,
        IEliminacionEmpresaRepositorio eliminacionRepositorio,
        IAlmacenamientoArchivos almacenamiento)
    {
        _empresaRepositorio = empresaRepositorio;
        _eliminacionRepositorio = eliminacionRepositorio;
        _almacenamiento = almacenamiento;
    }

    /// <inheritdoc />
    public async Task<EmpresaDto> ActualizarAsync(int empresaId, ActualizarEmpresaDto datos)
    {
        var empresa = await _empresaRepositorio.ObtenerPorIdAsync(empresaId)
            ?? throw new InvalidOperationException("La empresa indicada no existe.");

        if (string.IsNullOrWhiteSpace(datos.Nombre))
        {
            throw new InvalidOperationException("El nombre de la empresa es obligatorio.");
        }

        if (!CifValidador.EsValido(datos.Cif))
        {
            throw new InvalidOperationException("El CIF de la empresa es obligatorio.");
        }

        if (!DireccionValidador.EsValida(datos.Direccion))
        {
            throw new InvalidOperationException("La dirección de la empresa es obligatoria.");
        }

        var conEseCif = await _empresaRepositorio.ObtenerPorCifAsync(datos.Cif);
        if (conEseCif is not null && conEseCif.EmpresaId != empresaId)
        {
            throw new InvalidOperationException("Ya existe otra empresa registrada con este CIF.");
        }

        empresa.Nombre = datos.Nombre.Trim();
        empresa.Cif = datos.Cif;
        empresa.Direccion = datos.Direccion;
        await _empresaRepositorio.GuardarCambiosAsync();

        return EmpresaMapper.AEmpresaDto(empresa);
    }

    /// <inheritdoc />
    public async Task EliminarAsync(int empresaId)
    {
        if (await _empresaRepositorio.ObtenerPorIdAsync(empresaId) is null)
        {
            throw new InvalidOperationException("La empresa indicada no existe.");
        }

        if (await _eliminacionRepositorio.TieneRutaEnCursoAsync(empresaId))
        {
            throw new InvalidOperationException("La empresa tiene una ruta en curso: espera a que finalice para eliminarla.");
        }

        foreach (var referencia in await _eliminacionRepositorio.EliminarAsync(empresaId))
        {
            await _almacenamiento.EliminarAsync(referencia);
        }
    }
}
