namespace TransportApp.Application.Interfaces;

/// <summary>
/// Guarda y recupera archivos binarios (por ahora, las fotografías de las
/// evidencias) fuera de la base de datos: la base solo conserva la
/// referencia devuelta. No decide reglas de negocio ni de autorización.
/// </summary>
public interface IAlmacenamientoArchivos
{
    /// <summary>Guarda el contenido en la carpeta indicada y devuelve su referencia (ruta relativa).</summary>
    Task<string> GuardarAsync(string carpeta, string extension, Stream contenido);

    /// <summary>Abre el archivo de la referencia, o <c>null</c> si no existe.</summary>
    Task<Stream?> AbrirAsync(string referencia);
}
