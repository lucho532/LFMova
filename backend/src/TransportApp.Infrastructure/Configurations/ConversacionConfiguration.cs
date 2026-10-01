using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TransportApp.Domain.Entities;

namespace TransportApp.Infrastructure.Configurations;

/// <summary>
/// Configura la persistencia de <see cref="Conversacion"/> y su relación
/// 1:0..1, única, con <see cref="ServicioPasajero"/>.
/// </summary>
public class ConversacionConfiguration : IEntityTypeConfiguration<Conversacion>
{
    /// <summary>Aplica la configuración de EF Core para <see cref="Conversacion"/>.</summary>
    public void Configure(EntityTypeBuilder<Conversacion> builder)
    {
        builder.HasKey(c => c.ConversacionId);

        builder.HasOne(c => c.ServicioPasajero)
            .WithOne(sp => sp.Conversacion)
            .HasForeignKey<Conversacion>(c => c.ServicioPasajeroId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(c => c.ServicioPasajeroId).IsUnique();
    }
}
