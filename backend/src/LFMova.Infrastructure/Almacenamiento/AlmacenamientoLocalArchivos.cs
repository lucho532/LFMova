using Microsoft.Extensions.Configuration;
using LFMova.Application.Interfaces;

namespace LFMova.Infrastructure.Almacenamiento;

/// <summary>
/// Implementa <see cref="IAlmacenamientoArchivos"/> guardando los archivos en
/// una carpeta del servidor (configurable con <c>Almacenamiento:RutaBase</c>;
/// por defecto <c>almacenamiento</c> junto a la aplicación). Es una solución
/// inicial: el almacenamiento definitivo (nube) puede sustituirla sin cambiar
/// a quien la usa. Nunca acepta rutas fuera de la carpeta base.
/// </summary>
public class AlmacenamientoLocalArchivos : IAlmacenamientoArchivos
{
    private readonly string _rutaBase;

    /// <summary>Crea el almacenamiento con la configuración de la aplicación.</summary>
    public AlmacenamientoLocalArchivos(IConfiguration configuracion)
    {
        _rutaBase = Path.GetFullPath(configuracion["Almacenamiento:RutaBase"] ?? "almacenamiento");
    }

    /// <inheritdoc />
    public async Task<string> GuardarAsync(string carpeta, string extension, Stream contenido)
    {
        var referencia = $"{carpeta}/{Guid.NewGuid():N}{extension}";
        var destino = ResolverRuta(referencia);
        Directory.CreateDirectory(Path.GetDirectoryName(destino)!);
        await using var salida = File.Create(destino);
        await contenido.CopyToAsync(salida);
        return referencia;
    }

    /// <inheritdoc />
    public Task<Stream?> AbrirAsync(string referencia)
    {
        var ruta = ResolverRuta(referencia);
        return Task.FromResult<Stream?>(File.Exists(ruta) ? File.OpenRead(ruta) : null);
    }

    /// <inheritdoc />
    public Task EliminarAsync(string referencia)
    {
        var ruta = ResolverRuta(referencia);
        if (File.Exists(ruta))
        {
            File.Delete(ruta);
        }

        return Task.CompletedTask;
    }

    private string ResolverRuta(string referencia)
    {
        var ruta = Path.GetFullPath(Path.Combine(_rutaBase, referencia));
        if (!ruta.StartsWith(_rutaBase, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Referencia de archivo no válida.");
        }

        return ruta;
    }
}
