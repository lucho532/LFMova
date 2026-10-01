using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TransportApp.Domain.Entities;

namespace TransportApp.Infrastructure.Configurations;

/// <summary>
/// Configura la persistencia de <see cref="Jornada"/> y su relación con
/// <see cref="Empresa"/>. <see cref="Jornada"/> no tiene relación propia con
/// <see cref="UnidadOperativa"/>: la asignación de unidad es individual por
/// <see cref="Servicio"/> (ver <see cref="ServicioConfiguration"/>).
/// </summary>
public class JornadaConfiguration : IEntityTypeConfiguration<Jornada>
{
    /// <summary>Aplica la configuración de EF Core para <see cref="Jornada"/>.</summary>
    public void Configure(EntityTypeBuilder<Jornada> builder)
    {
        builder.HasKey(j => j.JornadaId);

        builder.HasOne(j => j.Empresa)
            .WithMany(e => e.Jornadas)
            .HasForeignKey(j => j.EmpresaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(j => j.EmpresaId);
    }
}
