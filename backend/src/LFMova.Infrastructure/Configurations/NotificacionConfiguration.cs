using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LFMova.Domain.Entities;

namespace LFMova.Infrastructure.Configurations;

/// <summary>
/// Configura la persistencia de <see cref="Notificacion"/> y su relación con
/// el <see cref="Usuario"/> destinatario.
/// </summary>
public class NotificacionConfiguration : IEntityTypeConfiguration<Notificacion>
{
    /// <summary>Aplica la configuración de EF Core para <see cref="Notificacion"/>.</summary>
    public void Configure(EntityTypeBuilder<Notificacion> builder)
    {
        builder.HasKey(n => n.NotificacionId);

        builder.HasOne(n => n.Usuario)
            .WithMany(u => u.Notificaciones)
            .HasForeignKey(n => n.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(n => n.UsuarioId);
    }
}
