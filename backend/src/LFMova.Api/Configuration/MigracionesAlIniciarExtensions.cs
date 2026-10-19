using Microsoft.EntityFrameworkCore;
using LFMova.Infrastructure.Data;

namespace LFMova.Api.Configuration;

/// <summary>
/// Aplica las migraciones pendientes de Entity Framework Core al arrancar la API, de modo que cada despliegue
/// deje la base de datos al día sin pasos manuales. Solo se activa con la opción
/// <c>AplicarMigracionesAlIniciar</c> en <c>true</c> (en producción, como variable de entorno del servicio);
/// por defecto no hace nada. No crea migraciones ni decide cómo es el modelo de datos.
/// </summary>
public static class MigracionesAlIniciarExtensions
{
    /// <summary>
    /// Si <c>AplicarMigracionesAlIniciar</c> es <c>true</c>, aplica las migraciones pendientes antes de atender
    /// solicitudes. Si fallan, la excepción detiene el arranque: así el despliegue falla y Railway conserva
    /// la versión anterior en lugar de publicar una API que no coincide con la base de datos.
    /// </summary>
    public static async Task AplicarMigracionesSiCorrespondeAsync(this WebApplication aplicacion)
    {
        if (!aplicacion.Configuration.GetValue<bool>("AplicarMigracionesAlIniciar"))
        {
            return;
        }

        using var alcance = aplicacion.Services.CreateScope();
        var contexto = alcance.ServiceProvider.GetRequiredService<LFMovaDbContext>();
        var pendientes = (await contexto.Database.GetPendingMigrationsAsync()).ToList();
        if (pendientes.Count == 0)
        {
            return;
        }

        aplicacion.Logger.LogInformation("Aplicando {Cantidad} migraciones pendientes: {Migraciones}", pendientes.Count, string.Join(", ", pendientes));
        await contexto.Database.MigrateAsync();
    }
}
