using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TransportApp.Domain.Entities;

namespace TransportApp.Infrastructure.Configurations;

/// <summary>
/// Configura la persistencia de <see cref="TokenVerificacion"/>. No
/// configura reglas de negocio.
/// </summary>
public class TokenVerificacionConfiguration : IEntityTypeConfiguration<TokenVerificacion>
{
    /// <summary>Aplica la configuración de EF Core para <see cref="TokenVerificacion"/>.</summary>
    public void Configure(EntityTypeBuilder<TokenVerificacion> builder)
    {
        builder.HasKey(t => t.TokenVerificacionId);

        builder.Property(t => t.TokenHash).IsRequired();

        builder.HasOne(t => t.Usuario)
            .WithMany(u => u.TokensVerificacion)
            .HasForeignKey(t => t.UsuarioId);
    }
}
