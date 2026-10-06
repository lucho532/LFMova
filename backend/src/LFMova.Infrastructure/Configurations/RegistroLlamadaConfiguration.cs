using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LFMova.Domain.Entities;

namespace LFMova.Infrastructure.Configurations;

/// <summary>
/// Configura la persistencia de <see cref="RegistroLlamada"/> y su relación
/// con <see cref="ServicioPasajero"/>. A diferencia del resto del modelo, el
/// borrado es en cascada: una llamada no significa nada sin su pasajero, y
/// así quitar un pasajero de una ruta no falla porque el conductor ya lo
/// hubiera llamado.
/// </summary>
public class RegistroLlamadaConfiguration : IEntityTypeConfiguration<RegistroLlamada>
{
    /// <summary>Aplica la configuración de EF Core para <see cref="RegistroLlamada"/>.</summary>
    public void Configure(EntityTypeBuilder<RegistroLlamada> builder)
    {
        builder.ToTable("RegistrosLlamada");
        builder.HasKey(r => r.RegistroLlamadaId);

        builder.HasOne(r => r.ServicioPasajero)
            .WithMany()
            .HasForeignKey(r => r.ServicioPasajeroId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(r => r.ServicioPasajeroId);
    }
}
