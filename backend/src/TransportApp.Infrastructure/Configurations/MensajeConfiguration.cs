using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TransportApp.Domain.Entities;

namespace TransportApp.Infrastructure.Configurations;

/// <summary>
/// Configura la persistencia de <see cref="Mensaje"/> y sus relaciones con
/// <see cref="Conversacion"/> y con el <see cref="Usuario"/> remitente.
/// </summary>
public class MensajeConfiguration : IEntityTypeConfiguration<Mensaje>
{
    /// <summary>Aplica la configuración de EF Core para <see cref="Mensaje"/>.</summary>
    public void Configure(EntityTypeBuilder<Mensaje> builder)
    {
        builder.HasKey(m => m.MensajeId);

        builder.HasOne(m => m.Conversacion)
            .WithMany(c => c.Mensajes)
            .HasForeignKey(m => m.ConversacionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.Usuario)
            .WithMany(u => u.Mensajes)
            .HasForeignKey(m => m.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(m => m.ConversacionId);
    }
}
