using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TransportApp.Domain.Entities;

namespace TransportApp.Infrastructure.Configurations;

/// <summary>
/// Configura la persistencia de <see cref="Usuario"/>: clave primaria y
/// unicidad global de la cédula. No configura reglas de negocio.
/// </summary>
public class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    /// <summary>Aplica la configuración de EF Core para <see cref="Usuario"/>.</summary>
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        builder.HasKey(u => u.UsuarioId);

        builder.Property(u => u.Cedula).IsRequired();

        builder.HasIndex(u => u.Cedula).IsUnique();

        builder.Property(u => u.NombreCompleto).IsRequired();

        builder.Property(u => u.Telefono).IsRequired();

        builder.HasIndex(u => u.Email).IsUnique();
    }
}
