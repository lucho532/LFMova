using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TransportApp.Domain.Entities;

namespace TransportApp.Infrastructure.Configurations;

/// <summary>
/// Configura la persistencia de <see cref="Empresa"/>. Las relaciones hacia
/// las demás entidades se configuran desde el lado dependiente (por ejemplo,
/// <c>SedeConfiguration</c>).
/// </summary>
public class EmpresaConfiguration : IEntityTypeConfiguration<Empresa>
{
    /// <summary>Aplica la configuración de EF Core para <see cref="Empresa"/>.</summary>
    public void Configure(EntityTypeBuilder<Empresa> builder)
    {
        builder.HasKey(e => e.EmpresaId);

        builder.Property(e => e.Nombre).IsRequired();

        builder.Property(e => e.Cif).IsRequired();

        builder.HasIndex(e => e.Cif).IsUnique();

        builder.Property(e => e.Direccion).IsRequired();
    }
}
