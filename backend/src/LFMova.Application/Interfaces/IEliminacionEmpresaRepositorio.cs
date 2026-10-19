namespace LFMova.Application.Interfaces;

/// <summary>
/// Persistencia del borrado completo de una empresa, dentro de una
/// transacción y en el orden que exigen las claves foráneas. No decide si la
/// empresa puede eliminarse: eso lo resuelve <see cref="IEdicionEmpresaServicio"/>.
/// </summary>
public interface IEliminacionEmpresaRepositorio
{
    /// <summary>Indica si la empresa tiene alguna ruta en curso.</summary>
    Task<bool> TieneRutaEnCursoAsync(int empresaId);

    /// <summary>
    /// Borra la empresa y todo lo que depende de ella. Las cuentas de sus
    /// personas se conservan sin empresa, salvo las que nunca se activaron y
    /// no tienen ninguna otra relación, que se borran. Devuelve las
    /// referencias de los archivos de evidencia que quedaron huérfanos.
    /// </summary>
    Task<List<string>> EliminarAsync(int empresaId);
}
